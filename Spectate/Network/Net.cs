using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using SNetwork;
using BitConverter = Il2CppSystem.BitConverter;
using Il2CppCollGen = Il2CppSystem.Collections.Generic;

namespace Spectate.Network;

/// <summary>
/// Provides low level data sending and receiving functions for a network.
/// </summary>
[HarmonyPatch]
public class Net {
	/// <summary>
	/// Key bytes that identifies a packet as belonging to our network.
	/// </summary>
	private const ushort NetKeyBytes = 0x5350; // "SP"

	/// <summary>
	/// SNet channel to send messages to
	/// In vanilla, 5 is for bot commands, others for everything else.
	/// </summary>
	private const int SNetChannel = 1;

	/// <summary>
	/// Handlers for different spectate message packet ids.
	///
	/// </summary>
	private static readonly Dictionary<byte /* packet id */,
		Action<byte[], SNet_Player? /* sender */>> MessageHandlers = new();

	/// <summary>
	/// Registers a handler for a spectate message of a given packet id.
	/// </summary>
	/// <param name="packetId">packet id to register to</param>
	/// <param name="handler">handler function</param>
	/// <returns>true if registered successfully, false if already exist</returns>
	public static bool RegisterHandler(byte packetId, Action<byte[], SNet_Player?> handler) {
		if (MessageHandlers.TryAdd(packetId, handler)) return true;
		Logger.Error($"[Net] Handler for packetId={packetId} is already registered to {MessageHandlers[packetId].Method.DeclaringType?.FullName ?? "???"}");
		return false;
	}

	/// <summary>
	/// Unregisters the handler for a spectate message of a given packet id.
	/// </summary>
	/// <param name="packetId">packet id to unregister</param>
	/// <returns>true if affected</returns>
	public static bool UnregisterHandler(byte packetId) {
		return MessageHandlers.Remove(packetId);
	}

	// =========================================================================
	// Receive
	// =========================================================================

	[HarmonyPatch(
		typeof(SNet_Replication),
		nameof(SNet_Replication.RecieveBytes)
	)]
	[HarmonyPrefix]
	private static bool OnReceive(Il2CppStructArray<byte> bytes, uint size, ulong messagerID) {
		if (!IsNetMessage(bytes)) {
			return true;
		}

		byte packetId = bytes[2];
		if (MessageHandlers.TryGetValue(packetId, out var handler)) {
			byte[] packetData = new byte[size - 3];
			Array.Copy(bytes, 3, packetData, 0, size - 3);
			SNet_Player? sender = NetHelper.GetPlayerByID(messagerID);
			handler(packetData, sender);
		} else {
			Logger.Error($"Net (key={NetKeyBytes:X}) received message for packetId={packetId} " +
			             $"with no handler, are we on the same version?");
		}

		return false;
	}

	// =========================================================================
	// Sending
	// =========================================================================

	/// <summary>
	/// Sends a message with given packet id and data to a target player.
	/// </summary>
	/// <param name="data">data to be sent</param>
	/// <param name="packetId">packet id to send to</param>
	/// <param name="target">player to send to</param>
	public static void SendBytes(byte[] data, byte packetId, SNet_Player target) {
		SendBytes(PrepareData(data, packetId), target);
	}

	/// <summary>
	/// Sends a message with given packet id and data to target players.
	/// </summary>
	/// <param name="data">data to be sent</param>
	/// <param name="packetId">packet id to send to</param>
	/// <param name="targets">players to send to</param>
	public static void SendBytes(byte[] data, byte packetId, Il2CppCollGen.List<SNet_Player> targets) {
		SendBytes(PrepareData(data, packetId), targets);
	}

	/// <summary>
	/// Sends a message with given raw bytes to a target player through <see cref="SNetChannel"/>.
	/// </summary>
	private static void SendBytes(byte[] bytes, SNet_Player target) {
		var cppBytes = new Il2CppStructArray<byte>(bytes);
		SNet.Core.SendBytes(cppBytes, SNet_SendQuality.Reliable, SNetChannel, target);
	}

	/// <summary>
	/// Sends a message with given raw bytes to target players through <see cref="SNetChannel"/>.
	/// </summary>
	private static void SendBytes(byte[] bytes, Il2CppCollGen.List<SNet_Player> targets) {
		var cppBytes = new Il2CppStructArray<byte>(bytes);
		SNet.Core.SendBytes(cppBytes, SNet_SendQuality.Reliable, SNetChannel, targets);
	}

	// =========================================================================
	// Helpers
	// =========================================================================

	/// <summary>
	/// Prepares a byte array to be sent as a spectate message by prepending
	/// the <see cref="NetKeyBytes"/> and <see cref="packetId"/> to the data.
	/// <br/>
	/// Encoding:
	/// [ <see cref="NetKeyBytes"/> 2B ] [ packetId 1B ] [ data ... ]
	/// </summary>
	/// <param name="data">data to send</param>
	/// <param name="packetId">packet id to encode</param>
	/// <returns>prepared data</returns>
	private static byte[] PrepareData(byte[] data, byte packetId = 0) {
		var bytes = new byte[3 + data.Length];
		BitConverter.GetBytes(NetKeyBytes).CopyTo(bytes, 0);
		bytes[2] = packetId;
		data.CopyTo(bytes, 3);
		return bytes;
	}

	/// <summary>
	/// Checks if a received byte array is a message belongs to this network.
	/// </summary>
	/// <param name="bytes">raw received bytes</param>
	/// <returns>true if is message for this network</returns>
	private static bool IsNetMessage(Il2CppStructArray<byte> bytes) {
		if (bytes.Length < 2) return false;
		ushort key = BitConverter.ToUInt16(bytes, 0);
		return key == NetKeyBytes;
	}
}

