using UnityEditor;

namespace CrimeVR.Editor
{
    [InitializeOnLoad]
    public static class AndroidApplicationEntryGuard
    {
        static AndroidApplicationEntryGuard()
        {
            EditorApplication.delayCall += EnsureValidAndroidApplicationEntry;
        }

        private static void EnsureValidAndroidApplicationEntry()
        {
            if (PlayerSettings.Android.applicationEntry == 0)
            {
                PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
