using System;
using System.Collections.Generic;
using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Нажимная плита. Считает нажатие вручную (AABB по габаритам тел), а не через PhysX:
    /// петля обязана считаться одинаково при любом состоянии синхронизации transform,
    /// а на плиту встаёт и игрок, и эхо — значит, плита не различает «своих».
    /// </summary>
    [DisallowMultipleComponent]
    public class PressurePlate : MonoBehaviour
    {
        readonly List<MeshRenderer> _padRenderers = new List<MeshRenderer>();

        Vector3 _center;
        Vector3 _size;

        Transform _pad;
        float _restPadY;
        float _anim;

        public int Index { get; private set; }
        public bool IsPressed { get; private set; }

        public event Action<PressurePlate, bool> StateChanged;

        public static PressurePlate Create(Transform parent, int index, Vector3 worldCenter, Vector3 worldSize)
        {
            var go = new GameObject("Plate_" + index);
            go.transform.SetParent(parent, false);
            go.transform.position = worldCenter;

            var plate = go.AddComponent<PressurePlate>();
            plate.Index = index;
            plate._center = worldCenter;
            plate._size = worldSize;
            plate.BuildVisual(worldCenter, worldSize);
            return plate;
        }

        void BuildVisual(Vector3 center, Vector3 size)
        {
            float w = size.x;
            float d = size.z;

            // Рамка-подставка утапливается в пол, чтобы плиту было видно с любой высоты.
            float frameHeight = 0.34f;
            Primitives.Box(transform, "Frame",
                new Vector3(0f, -size.y * 0.5f - 0.05f, 0f),
                new Vector3(w + 0.26f, frameHeight, d + 0.26f),
                MaterialLibrary.Lit(Palette.StructureDark, 0.2f, 0.25f));

            var pad = Primitives.Box(transform, "Pad", Vector3.zero,
                new Vector3(w, 0.18f, d), MaterialLibrary.Glowing(Palette.Accent, 1.4f));
            _pad = pad.transform;
            _restPadY = _pad.localPosition.y;
            _padRenderers.AddRange(pad.GetComponentsInChildren<MeshRenderer>());

            // Углы рамки задают габарит плиты, даже когда сама площадка утоплена.
            var cornerMat = MaterialLibrary.Lit(Palette.Dim(Palette.Power, 0.25f), 0.45f, 0.3f);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Primitives.Box(transform, "Corner",
                        new Vector3(sx * (w * 0.5f - 0.12f), -0.06f, sz * (d * 0.5f - 0.12f)),
                        new Vector3(0.24f, 0.3f, 0.24f), cornerMat, false);
                }
            }

            _pad.localPosition = new Vector3(0f, _restPadY - 0.16f, 0f);
        }

        public bool Evaluate(IReadOnlyList<LoopActor> actors)
        {
            var box = new Bounds(_center, _size);

            bool pressed = false;
            for (int i = 0; i < actors.Count; i++)
            {
                var a = actors[i];
                if (a != null && box.Intersects(a.Bounds))
                {
                    pressed = true;
                    break;
                }
            }

            if (pressed != IsPressed)
            {
                IsPressed = pressed;
                StateChanged?.Invoke(this, pressed);
            }

            return pressed;
        }

        public void ResetState()
        {
            IsPressed = false;
            _anim = 0f;
            if (_pad != null) _pad.localPosition = new Vector3(0f, _restPadY - 0.16f, 0f);
        }

        void Update()
        {
            _anim = Mathf.MoveTowards(_anim, IsPressed ? 1f : 0f, Time.deltaTime * 9f);

            if (_pad != null)
                _pad.localPosition = new Vector3(0f, _restPadY - 0.16f * _anim, 0f);

            Color tint = Color.Lerp(Palette.Dim(Palette.Accent, 0.5f), Palette.Accent, _anim);
            var mat = MaterialLibrary.Glowing(tint, Mathf.Lerp(1.0f, 3.4f, _anim));

            for (int i = 0; i < _padRenderers.Count; i++)
                if (_padRenderers[i] != null) _padRenderers[i].sharedMaterial = mat;
        }
    }
}