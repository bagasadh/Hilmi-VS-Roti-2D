using System.Text;
using HilmiVSRoti.Items;
using HilmiVSRoti.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HilmiVSRoti.UI
{
    /// <summary>
    /// Menampilkan daftar efek roti yang sedang aktif beserta sisa durasinya di UI.
    /// </summary>
    public class BreadUI : MonoBehaviour
    {
        [SerializeField] private PlayerBreadEffects breadEffects;
        [SerializeField] private Text breadStatusText;

        private readonly StringBuilder sb = new StringBuilder();

        private void OnEnable()
        {
            if (breadEffects != null)
                breadEffects.OnEffectsUpdated += RefreshDisplay;
        }

        private void OnDisable()
        {
            if (breadEffects != null)
                breadEffects.OnEffectsUpdated -= RefreshDisplay;
        }

        private void Start()
        {
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (breadStatusText == null || breadEffects == null)
                return;

            var active = breadEffects.GetActiveEffects();
            if (active.Count == 0)
            {
                breadStatusText.text = "";
                return;
            }

            sb.Clear();
            foreach (var kvp in active)
                sb.AppendLine($"{GetLabel(kvp.Key)}: {kvp.Value:0.0}s");

            breadStatusText.text = sb.ToString().TrimEnd();
        }

        private static string GetLabel(BreadType type) => type switch
        {
            BreadType.RotiTawar => "[BUFF] Roti Tawar (Lompat Tinggi)",
            BreadType.RotiKeju => "[BUFF] Roti Keju (Mode Tabrak!)",
            BreadType.RotiCokelat => "[BUFF] Roti Cokelat (Lari Cepat)",
            BreadType.RotiSobek => "[BUFF] Roti Sobek (Perisai 1x)",
            BreadType.RotiJamuran => "[DEBUFF] Roti Jamuran (Lambat!)",
            _ => type.ToString()
        };
    }
}

