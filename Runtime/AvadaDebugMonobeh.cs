#if AVADA_ENABLE_SCREEN_DEBUG
using System.Text;
using UnityEngine;

namespace AvadaKedavrav2
{
    public class AvadaDebugMonobeh : MonoBehaviour
    {
        public StringBuilder rs;

        private void OnGUI()
        {
            GUI.Label(new Rect(50f, 50f, 400f, rs.Length * 25f), rs.ToString());
        }
    }
}
#endif