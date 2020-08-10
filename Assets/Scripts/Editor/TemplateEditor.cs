using UnityEngine;
using UnityEditor;
using KotORVR;

namespace KotORUnity
{
    [CustomEditor(typeof(Item))]
    public class ItemEditor : TemplateEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        public override bool HasPreviewGUI()
        {
            targetMeshFilters = ((Item)target).GetComponentsInChildren<MeshFilter>();
            targetMeshRenderers = ((Item)target).GetComponentsInChildren<MeshRenderer>();
            targetIconTexture = ((Item)target).icon.texture;

            return base.HasPreviewGUI();
        }
    }

    [CustomEditor(typeof(Placeable))]
    public class PlaceableEditor : TemplateEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        public override bool HasPreviewGUI()
        {
            targetMeshFilters = ((Placeable)target).GetComponentsInChildren<MeshFilter>();
            targetMeshRenderers = ((Placeable)target).GetComponentsInChildren<MeshRenderer>();

            return base.HasPreviewGUI();
        }
    }

    [CustomEditor(typeof(Character))]
    public class CharacterEditor : TemplateEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        public override bool HasPreviewGUI()
        {
            targetMeshFilters = ((Character)target).GetComponentsInChildren<MeshFilter>();
            targetMeshRenderers = ((Character)target).GetComponentsInChildren<MeshRenderer>();

            return base.HasPreviewGUI();
        }
    }

    [CustomEditor(typeof(Door))]
    public class DoorEditor : TemplateEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        public override bool HasPreviewGUI()
        {
            targetMeshFilters = ((Door)target).GetComponentsInChildren<MeshFilter>();
            targetMeshRenderers = ((Door)target).GetComponentsInChildren<MeshRenderer>();

            return base.HasPreviewGUI();
        }
    }

    [CustomEditor(typeof(TemplateObject), true)]
    public class TemplateEditor : Editor
    {
        private Vector2 scroll;
        private bool showJSON;

        protected PreviewRenderUtility previewRenderUtility;
        protected MeshFilter[] targetMeshFilters;
        protected MeshRenderer[] targetMeshRenderers;
        protected Texture targetIconTexture;

        public override void OnInspectorGUI()
        {
            TemplateObject t = (TemplateObject)target;

            showJSON = EditorGUILayout.Foldout(showJSON, "GFF Template JSON");

            if (showJSON) {
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(200));
                EditorGUILayout.TextArea(t.GetJSON());
                EditorGUILayout.EndScrollView();
            }
        }

        public override bool HasPreviewGUI()
        {
            if (previewRenderUtility == null) {
                previewRenderUtility = new PreviewRenderUtility();

                previewRenderUtility.camera.transform.position = new Vector3(0, 0, -4);
                previewRenderUtility.camera.transform.rotation = Quaternion.identity;
            }

            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint) {
                previewRenderUtility.BeginPreview(r, background);
                
                for (int i = 0; i < targetMeshFilters?.Length && i < targetMeshRenderers?.Length; i++) {
                    previewRenderUtility.DrawMesh(targetMeshFilters[i].sharedMesh, targetMeshFilters[i].transform.position, targetMeshFilters[i].transform.rotation, targetMeshRenderers[i].sharedMaterial, 0);

                }
                
                previewRenderUtility.camera.Render();
                Texture render = previewRenderUtility.EndPreview();

                //Draw the model render
                GUI.DrawTexture(r, render, ScaleMode.ScaleToFit);

                //Draw the icon in the lower right corner
                float len = r.height / 2;
                r = new Rect(r.xMax - len, r.yMax - len, len, len);
                GUI.DrawTexture(r, targetIconTexture, ScaleMode.ScaleToFit);
            }
        }


        private void OnDestroy()
        {
            previewRenderUtility.Cleanup();
        }
    }
}