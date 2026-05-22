using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using SNetwork;
using BitConverter = Il2CppSystem.BitConverter;
using Il2CppCollGen = Il2CppSystem.Collections.Generic;

namespace Spectate.Network;

/// <summary>
/// Provides low level data sending and receiving functions.
/// </summary>
[HarmonyPatch]
public class Net {
	private const ushort KeyBytesSpectate = 0x5350; // "SP"
	private const int SNetChannelSpectate = 1;

	private static readonly Dictionary<byte /* packet index */,
		Action<byte[] /* packet data */, SNet_Player? /* sender */>> SpectateMessageHandlers = new();

	/// <summary>
	/// Registers a handler for a spectate message of a given packet index.
	/// </summary>
	/// <param name="packetIndex">packet index to register to</param>
	/// <param name="handler">handler function</param>
	/// <returns>true if registered successfully, false if already exist</returns>
	public static bool RegisterHandler(byte packetIndex, Action<byte[] /* packet data */, SNet_Player? /* sender */> handler) {
		if (SpectateMessageHandlers.TryAdd(packetIndex, handler)) return true;
		Logger.Error($"Handler for packet index {packetIndex} is already registered!");
		return false;
	}

	/// <summary>
	/// Unregisters the handler for a spectate message of a given packet index.
	/// </summary>
	/// <param name="packetIndex">packet index to unregister</param>
	/// <returns>true if affected</returns>
	public static bool UnregisterHandler(byte packetIndex) {
		return SpectateMessageHandlers.Remove(packetIndex);
	}

	// =================================================================================================================
	// Receive
	// =================================================================================================================

	[HarmonyPatch(
		typeof(SNet_Replication),
		nameof(SNet_Replication.RecieveBytes)
	)]
	[HarmonyPrefix]
	private static bool OnReceive(Il2CppStructArray<byte> bytes, uint size, ulong messagerID) {
		if (!IsSpectateMessage(bytes)) {
			return true;
		}

		byte packetIndex = bytes[2];
		if (SpectateMessageHandlers.TryGetValue(packetIndex, out var handler)) {
			byte[] packetData = new byte[size - 3];
			Array.Copy(bytes, 3, packetData, 0, size - 3);
			SNet_Player? sender = GetPlayerByID(messagerID);
			handler(packetData, sender);
		} else {
			Logger.Error($"Net received message (packetIdx={packetIndex}) with no handler, are we on the same version?");
		}

		return false;
	}

	// =================================================================================================================
	// Sending
	// =================================================================================================================

	/// <summary>
	/// Sends a spectate message with given packet index and data to a target player.
	/// </summary>
	/// <param name="data">data to be sent</param>
	/// <param name="packetIndex">packet index to send to</param>
	/// <param name="target">player to send to</param>
	public static void SendBytes(byte[] data, byte packetIndex, SNet_Player target) {
		SendBytes(PrepareData(data, packetIndex), target);
	}

	/// <summary>
	/// Sends a spectate message with given packet index and data to target players.
	/// </summary>
	/// <param name="data">data to be sent</param>
	/// <param name="packetIndex">packet index to send to</param>
	/// <param name="targets">players to send to</param>
	public static void SendBytes(byte[] data, byte packetIndex, Il2CppCollGen.List<SNet_Player> targets) {
		SendBytes(PrepareData(data, packetIndex), targets);
	}

	/// <summary>
	/// Sends a spectate message with given raw bytes to a target player through <see cref="SNetChannelSpectate"/>.
	/// </summary>
	private static void SendBytes(byte[] bytes, SNet_Player target) {
		var cppBytes = new Il2CppStructArray<byte>(bytes);
		SNet.Core.SendBytes(cppBytes, SNet_SendQuality.Reliable, SNetChannelSpectate, target);
	}

	/// <summary>
	/// Sends a spectate message with given raw bytes to target players through <see cref="SNetChannelSpectate"/>.
	/// </summary>
	private static void SendBytes(byte[] bytes, Il2CppCollGen.List<SNet_Player> targets) {
		var cppBytes = new Il2CppStructArray<byte>(bytes);
		SNet.Core.SendBytes(cppBytes, SNet_SendQuality.Reliable, SNetChannelSpectate, targets);
	}

	// =================================================================================================================
	// Helpers
	// =================================================================================================================

	/// <summary>
	/// Prepares a byte array to be sent as a spectate message by adding the spectate key and packet index at the beginning.
	/// </summary>
	/// <param name="data">data to send</param>
	/// <param name="packetIndex">packet index to encode</param>
	/// <returns>prepared data</returns>
	private static byte[] PrepareData(byte[] data, byte packetIndex = 0) {
		var bytes = new byte[3 + data.Length];
		BitConverter.GetBytes(KeyBytesSpectate).CopyTo(bytes, 0);
		bytes[2] = packetIndex;
		data.CopyTo(bytes, 3);
		return bytes;
	}

	/// <summary>
	/// Checks if a received byte array is a spectate message by verifying the spectate key at the beginning.
	/// </summary>
	/// <param name="bytes">raw received bytes</param>
	/// <returns>true if is spectate message</returns>
	private static bool IsSpectateMessage(Il2CppStructArray<byte> bytes) {
		if (bytes.Length < 2) return false;
		ushort key = BitConverter.ToUInt16(bytes, 0);
		return key == KeyBytesSpectate;
	}

	/// <summary>
	/// Finds a player by their lookup ID (i.e. steamid) from the list of players in the current level.
	/// </summary>
	/// <param name="id">player lookup (i.e. steamid)</param>
	/// <returns>player matched, otherwise false</returns>
	private static SNet_Player? GetPlayerByID(ulong id) {
		foreach (var agent in SNet.LobbyPlayers) {
			if (agent.Lookup == id) {
				return agent;
			}
		}

		return null;
	}
}

