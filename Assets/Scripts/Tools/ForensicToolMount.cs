using UnityEngine;

namespace CrimeVR.Tools
{
    public class ForensicToolMount : MonoBehaviour
    {
        [SerializeField] private string mountId = "tool.mount.primary";
        [SerializeField] private Transform mountPoint;
        [SerializeField] private GameObject equippedTool;

        public string MountId => mountId;
        public Transform MountPoint => mountPoint;
        public GameObject EquippedTool => equippedTool;

        private void Reset()
        {
            mountPoint = transform;
        }

        public void Equip(GameObject toolInstance)
        {
            equippedTool = toolInstance;
            if (equippedTool == null || mountPoint == null)
                return;

            equippedTool.transform.SetParent(mountPoint, false);
            equippedTool.transform.localPosition = Vector3.zero;
            equippedTool.transform.localRotation = Quaternion.identity;
        }
    }
}
