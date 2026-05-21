using Spectate.Config;
using Spectate.Interop;
using TMPro;
using UnityEngine;

namespace Spectate.UI;

[RegisterIl2Cpp]
public class Wm : MonoBehaviour {
	private const float Itv = 0.01f;

	private TextMeshPro? _tp;
	private byte _fx = 77;
	private byte _dx = 42;
	private float _t;
	private int _ma;
	private int _mj;
	private int _mi;

	public Wm(IntPtr ptr) : base(ptr) {
	}

	private void Awake() {
		Ld();
		Mk();
	}

	private void Update() {
		if (_tp == null && !Mk()) return;
		if (!Util.SetTargetActiveIfDiff(_tp!.gameObject, !ConfigMgr.HideWm)) return;
		_t += Time.deltaTime;
		if (_t < Itv) return;
		_t = 0.0f;
		_tp!.text = $"TKN[SPX{_ma:X2}{_fx:X2}{_mj:X2}{_dx:X2}{_mi:X2}]";
		Nx();
	}

	private void Ld() {
		var l = Plugin.VERSION.Split('.').Select(int.Parse).ToArray();
		_ma = l[0];
		_mj = l[1];
		_mi = l[2];
	}

	private void Nx() {
		_fx = (byte)((_fx + 1) % 256);
		_dx = (byte)((_dx + 1) % 256);
	}

	private bool Mk() {
		Transform? r = GuiManager.WatermarkLayer?.m_watermark?.transform.parent;
		TextMeshPro? t = GuiManager.WatermarkLayer?.m_watermark?.m_fpsText;
		if (r == null || t == null) {
			return false;
		}

		_tp = Instantiate(t, r, false);
		_tp.name = "SPWm";
		_tp.rectTransform.anchorMax = new Vector2(1.0f, 0.22f);
		_tp.rectTransform.anchorMin = new Vector2(1.0f, 0.22f);
		_tp.rectTransform.pivot = new Vector2(0.5f, 0.0f);
		_tp.rectTransform.anchoredPosition3D = new Vector3(-10f, 0.0f, 0.0f);
		_tp.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
		_tp.rectTransform.sizeDelta = new Vector2(200.0f, 12.0f);
		_tp.enableAutoSizing = true;
		_tp.overflowMode = TextOverflowModes.Overflow;
		_tp.alignment = TextAlignmentOptions.Center;
		_tp.gameObject.SetActive(!ConfigMgr.HideWm);
		return true;
	}
}
