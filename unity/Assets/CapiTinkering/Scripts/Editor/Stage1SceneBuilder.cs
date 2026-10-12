using System;
using System.IO;
using System.Linq;
using CapiTinkering.Game;
using CapiTinkering.Simulation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CapiTinkering.EditorTools
{
    /// <summary>
    /// Cria os assets de dados e a cena da etapa 1 com todas as referências ligadas.
    /// Menu: Capi Tinkering ▸ Etapa 1 ▸ Criar dados e cena.
    /// Pode ser executado de novo: reaproveita assets existentes e recria a cena.
    /// </summary>
    public static class Stage1SceneBuilder
    {
        private const string Root = "Assets/CapiTinkering";
        private const string DataFolder = Root + "/Data";
        private const string MaterialsFolder = DataFolder + "/Materials";
        private const string ScenesFolder = Root + "/Scenes";
        private const string ScenePath = ScenesFolder + "/Etapa1_Energia.unity";
        private const string ArtFolder = Root + "/Art";
        private const string PortraitPath = ArtFolder + "/Capi_Portrait.png";
        private const string LogoPath = ArtFolder + "/CapyTinkering_Logo.png";

        [MenuItem("Capi Tinkering/Etapa 1/Criar dados e cena", priority = 0)]
        public static void BuildAll()
        {
            if (!TmpResourcesAvailable())
            {
                EditorUtility.DisplayDialog("TextMesh Pro",
                    "Importe primeiro os recursos essenciais do TextMesh Pro:\n" +
                    "Window ▸ TextMeshPro ▸ Import TMP Essential Resources.\nDepois execute este menu novamente.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var mission = CreateOrUpdateData();
            BuildScene(mission);
        }

        [MenuItem("Capi Tinkering/Etapa 1/Criar apenas dados", priority = 1)]
        public static void BuildDataOnly()
        {
            var mission = CreateOrUpdateData();
            Selection.activeObject = mission;
            EditorGUIUtility.PingObject(mission);
        }

        // ---------------------------------------------------------------- Dados

        private static MissionDefinition CreateOrUpdateData()
        {
            EnsureFolder(DataFolder);

            var lifeSupport = LoadOrCreate<EquipmentDefinition>(DataFolder + "/Equipment_LifeSupport.asset");
            lifeSupport.Configure(EquipmentIds.LifeSupport, "equip.life_support", EquipmentRole.LifeSupport, 1.0f, 0, false, true);
            var communication = LoadOrCreate<EquipmentDefinition>(DataFolder + "/Equipment_Communication.asset");
            communication.Configure(EquipmentIds.Communication, "equip.communication", EquipmentRole.Communication, 0.5f, 1, true, true);
            var research = LoadOrCreate<EquipmentDefinition>(DataFolder + "/Equipment_Research.asset");
            research.Configure(EquipmentIds.Research, "equip.research", EquipmentRole.Research, 2.0f, 2, true, true);

            var mission = LoadOrCreate<MissionDefinition>(DataFolder + "/Mission_Stage1_Energy.asset");
            mission.SetEquipment(new[] { lifeSupport, communication, research });

            foreach (var asset in new UnityEngine.Object[] { lifeSupport, communication, research, mission })
                EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            if (!mission.TryValidate(out var error))
                Debug.LogError($"[Capi] Missão criada, mas inválida: {error}", mission);
            else
                Debug.Log("[Capi] Dados da etapa 1 prontos em " + DataFolder, mission);

            return mission;
        }

        // ---------------------------------------------------------------- Cena

        private static void BuildScene(MissionDefinition mission)
        {
            EnsureFolder(ScenesFolder);
            EnsureFolder(MaterialsFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Câmera isométrica fixa (ortográfica, 30° / 45°)
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BrandPalette.NavyDeep;
            cameraGo.transform.SetPositionAndRotation(new Vector3(-10f, 10f, -10f), Quaternion.Euler(35.264f, 45f, 0f));
            // Desloca a base para a direita da tela, deixando espaço ao painel à esquerda
            cameraGo.transform.position += cameraGo.transform.right * -3.5f;
            cameraGo.AddComponent<AudioListener>();

            var sunGo = new GameObject("Sun (Directional Light)");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.2f, 0.26f);

            // Base lunar com formas simples
            var matGround = CreateMaterial("M_Regolith", new Color(0.55f, 0.55f, 0.58f));
            var matHabitat = CreateMaterial("M_Habitat", Color.white);
            var matPanel = CreateMaterial("M_SolarPanel", new Color(0.12f, 0.22f, 0.45f));
            var matBattery = CreateMaterial("M_BatteryCase", new Color(0.25f, 0.27f, 0.3f));
            var matBatteryFill = CreateMaterial("M_BatteryFill", BrandPalette.Orange);
            var matModule = CreateMaterial("M_Module", BrandPalette.Turquoise);

            var baseRoot = new GameObject("LunarBase").transform;
            Primitive(PrimitiveType.Plane, "Ground", baseRoot, Vector3.zero, new Vector3(2f, 1f, 2f), matGround);
            Primitive(PrimitiveType.Cylinder, "Habitat", baseRoot, new Vector3(0f, 0.75f, 0f), new Vector3(2f, 0.75f, 2f), matHabitat);

            var panels = new GameObject("SolarArray").transform;
            panels.SetParent(baseRoot, false);
            for (int i = 0; i < 3; i++)
            {
                var panel = Primitive(PrimitiveType.Cube, "SolarPanel_" + (i + 1), panels,
                    new Vector3(-4f, 0.9f, -2f + i * 1.6f), new Vector3(1.4f, 0.05f, 1.2f), matPanel);
                panel.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
                Primitive(PrimitiveType.Cylinder, "Mast", panels, new Vector3(-4f, 0.4f, -2f + i * 1.6f), new Vector3(0.1f, 0.4f, 0.1f), matBattery);
            }

            Primitive(PrimitiveType.Cube, "BatteryCase", baseRoot, new Vector3(2.6f, 0.6f, -2.2f), new Vector3(1.0f, 1.2f, 0.8f), matBattery);
            var fillPivot = new GameObject("BatteryFillPivot").transform;
            fillPivot.SetParent(baseRoot, false);
            fillPivot.localPosition = new Vector3(2.6f, 0.02f, -2.65f);
            var fill = Primitive(PrimitiveType.Cube, "BatteryFill", fillPivot, new Vector3(0f, 0.5f, 0f), new Vector3(0.8f, 1f, 0.05f), matBatteryFill);
            UnityEngine.Object.DestroyImmediate(fill.GetComponent<Collider>());
            fillPivot.localScale = new Vector3(1f, 1.1f, 1f);

            var research = Primitive(PrimitiveType.Cube, "ResearchModule", baseRoot, new Vector3(2.4f, 0.5f, 1.8f), new Vector3(1.6f, 1f, 1.2f), matModule);
            var antenna = Primitive(PrimitiveType.Cylinder, "CommAntenna", baseRoot, new Vector3(0f, 2.3f, 0f), new Vector3(0.12f, 0.8f, 0.12f), matModule);

            // Controlador
            var controllerGo = new GameObject("MissionController");
            var controller = controllerGo.AddComponent<MissionController>();
            SetRef(controller, "mission", mission);

            var view = controllerGo.AddComponent<PowerSceneView>();
            SetRef(view, "controller", controller);
            SetRef(view, "sunLight", sun);
            SetRef(view, "batteryFill", fillPivot);
            SetFloat(view, "batteryFullHeight", 1.1f);
            SetRef(view, "researchModule", research.GetComponent<Renderer>());
            SetRef(view, "communicationAntenna", antenna.GetComponent<Renderer>());

            BuildUi(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Debug.Log("[Capi] Cena criada: " + ScenePath + ". Pressione Play para testar.");
        }

        // ---------------------------------------------------------------- Interface

        private static void BuildUi(MissionController controller)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateEventSystem();

            // Painel esquerdo: estado da missão
            var left = Panel("MissionPanel", canvasGo.transform, BrandPalette.Navy, 0.92f);
            Anchor(left, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(760f, -48f));
            var layout = Vertical(left.gameObject, 10, new RectOffset(24, 24, 24, 24));
            layout.childAlignment = TextAnchor.UpperLeft;

            // Linha de título: logo oficial + nome da missão (texto vem da TextTable)
            var titleRow = new GameObject("TitleRow", typeof(RectTransform)).GetComponent<RectTransform>();
            titleRow.SetParent(left, false);
            var titleLayout = titleRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 16f;
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = false;
            titleLayout.childForceExpandHeight = false;
            titleRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
            var logoSprite = LoadSprite(LogoPath);
            if (logoSprite != null)
            {
                var logo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
                logo.transform.SetParent(titleRow, false);
                var logoImage = logo.GetComponent<Image>();
                logoImage.sprite = logoSprite;
                logoImage.preserveAspect = true;
                logoImage.raycastTarget = false;
                var logoLayout = logo.AddComponent<LayoutElement>();
                logoLayout.preferredWidth = 96f;
                logoLayout.preferredHeight = 96f;
            }
            var title = Text("Title", titleRow, "", 28, FontStyles.Bold, BrandPalette.Cream);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var turn = Text("TurnText", left, "", 24, FontStyles.Bold, BrandPalette.White);
            var objective = Text("ObjectiveText", left, "", 22, FontStyles.Normal, BrandPalette.White);
            var outcome = Text("OutcomeText", left, "", 22, FontStyles.Italic, BrandPalette.Orange);
            var energy = Text("EnergyText", left, "", 22, FontStyles.Normal, BrandPalette.White);

            var barBg = Panel("BatteryBar", left, BrandPalette.NavyLight, 1f);
            barBg.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            var barFill = Panel("BatteryBarFill", barBg, BrandPalette.Orange, 1f);
            Stretch(barFill);
            var fillImage = barFill.GetComponent<Image>();
            fillImage.sprite = WhiteSprite();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

            var flow = Text("FlowText", left, "", 20, FontStyles.Normal, BrandPalette.Muted);
            var systems = Text("SystemsText", left, "", 22, FontStyles.Normal, BrandPalette.White);
            var forecast = Text("ForecastText", left, "", 20, FontStyles.Normal, BrandPalette.Turquoise);

            var toggles = new GameObject("EquipmentButtons", typeof(RectTransform)).GetComponent<RectTransform>();
            toggles.SetParent(left, false);
            var row = toggles.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12f;
            row.childForceExpandWidth = true;
            row.childControlWidth = true;
            row.childControlHeight = true;
            toggles.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            var commButton = ButtonWithLabel("Toggle_Communication", toggles, BrandPalette.NavyLight, BrandPalette.White, out var commLabel);
            var researchButton = ButtonWithLabel("Toggle_Research", toggles, BrandPalette.NavyLight, BrandPalette.White, out var researchLabel);

            var actions = new GameObject("ActionButtons", typeof(RectTransform)).GetComponent<RectTransform>();
            actions.SetParent(left, false);
            var actionRow = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionRow.spacing = 12f;
            actionRow.childForceExpandWidth = true;
            actionRow.childControlWidth = true;
            actionRow.childControlHeight = true;
            actions.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
            // Texto azul-marinho sobre laranja (6,2:1); branco sobre laranja teria só 2,7:1
            var advance = ButtonWithLabel("AdvanceTurnButton", actions, BrandPalette.Orange, BrandPalette.Navy, out var advanceLabel);
            var restart = ButtonWithLabel("RestartButton", actions, BrandPalette.NavyLight, BrandPalette.White, out var restartLabel);

            var options = new GameObject("OptionButtons", typeof(RectTransform)).GetComponent<RectTransform>();
            options.SetParent(left, false);
            var optionRow = options.gameObject.AddComponent<HorizontalLayoutGroup>();
            optionRow.spacing = 12f;
            optionRow.childForceExpandWidth = true;
            optionRow.childControlWidth = true;
            optionRow.childControlHeight = true;
            options.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
            var language = ButtonWithLabel("LanguageButton", options, BrandPalette.NavyLight, BrandPalette.White, out var languageLabel);
            var motion = ButtonWithLabel("ReduceMotionButton", options, BrandPalette.NavyLight, BrandPalette.White, out var motionLabel);

            // Painel do Capi (inferior direito)
            var capi = Panel("CapiPanel", canvasGo.transform, BrandPalette.NavyLight, 0.95f);
            Anchor(capi, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(1060f, 230f));
            var capiRow = capi.gameObject.AddComponent<HorizontalLayoutGroup>();
            capiRow.padding = new RectOffset(20, 20, 20, 20);
            capiRow.spacing = 20f;
            capiRow.childControlWidth = true;
            capiRow.childControlHeight = true;
            capiRow.childForceExpandWidth = false;
            capiRow.childForceExpandHeight = true;

            // Retrato 2D do Capi (Art/Capi_Portrait.png). Sem a imagem, usa um bloco turquesa com o nome.
            var portrait = Panel("CapiPortrait", capi, BrandPalette.Turquoise, 1f);
            var portraitLayout = portrait.gameObject.AddComponent<LayoutElement>();
            portraitLayout.preferredWidth = 190f;
            portraitLayout.flexibleWidth = 0f;
            var portraitSprite = LoadSprite(PortraitPath);
            if (portraitSprite != null)
            {
                var portraitImage = portrait.GetComponent<Image>();
                portraitImage.sprite = portraitSprite;
                portraitImage.color = Color.white;
                portraitImage.preserveAspect = true;
            }
            else
            {
                var portraitLabel = Text("PortraitLabel", portrait, "CAPI", 40, FontStyles.Bold, BrandPalette.Navy);
                portraitLabel.alignment = TextAlignmentOptions.Center;
                Stretch(portraitLabel.rectTransform);
            }

            var capiTexts = new GameObject("CapiTexts", typeof(RectTransform)).GetComponent<RectTransform>();
            capiTexts.SetParent(capi, false);
            Vertical(capiTexts.gameObject, 8, new RectOffset(0, 0, 0, 0));
            capiTexts.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var capiSeverity = Text("CapiSeverityText", capiTexts, "", 24, FontStyles.Bold, BrandPalette.White);
            var capiMessage = Text("CapiMessageText", capiTexts, "", 22, FontStyles.Normal, BrandPalette.White);

            // HUD e ligações
            var hud = canvasGo.AddComponent<MissionHUD>();
            SetRef(hud, "controller", controller);
            SetRef(hud, "titleText", title);
            SetRef(hud, "turnText", turn);
            SetRef(hud, "objectiveText", objective);
            SetRef(hud, "energyText", energy);
            SetRef(hud, "flowText", flow);
            SetRef(hud, "systemsText", systems);
            SetRef(hud, "forecastText", forecast);
            SetRef(hud, "outcomeText", outcome);
            SetRef(hud, "capiPanel", capi.GetComponent<Image>());
            SetRef(hud, "capiSeverityText", capiSeverity);
            SetRef(hud, "capiMessageText", capiMessage);
            SetRef(hud, "batteryFill", fillImage);
            SetRef(hud, "advanceButton", advance);
            SetRef(hud, "advanceLabel", advanceLabel);
            SetRef(hud, "restartButton", restart);
            SetRef(hud, "restartLabel", restartLabel);
            SetRef(hud, "languageButton", language);
            SetRef(hud, "languageLabel", languageLabel);
            SetRef(hud, "motionButton", motion);
            SetRef(hud, "motionLabel", motionLabel);

            var so = new SerializedObject(hud);
            var list = so.FindProperty("equipmentToggles");
            list.arraySize = 2;
            FillToggle(list.GetArrayElementAtIndex(0), EquipmentIds.Communication, commButton, commLabel);
            FillToggle(list.GetArrayElementAtIndex(1), EquipmentIds.Research, researchButton, researchLabel);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void FillToggle(SerializedProperty element, string id, Button button, TMP_Text label)
        {
            element.FindPropertyRelative("equipmentId").stringValue = id;
            element.FindPropertyRelative("button").objectReferenceValue = button;
            element.FindPropertyRelative("label").objectReferenceValue = label;
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            // Usa o módulo do Input System quando o pacote existe (padrão nos modelos do Unity 6);
            // caso contrário, o módulo do Input Manager clássico. Reflexão evita dependência de compilação.
            var inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null) go.AddComponent(inputSystemModule);
            else go.AddComponent<StandaloneInputModule>();
        }

        // ---------------------------------------------------------------- Utilidades

        private static bool TmpResourcesAvailable() => Resources.Load<TMP_Settings>("TMP Settings") != null;

        /// <summary>Carrega uma imagem como Sprite, corrigindo o tipo de importação se necessário. Nulo se não existir.</summary>
        private static Sprite LoadSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[Capi] Imagem não encontrada ou não importada como Sprite: " + path);
            return sprite;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static RectTransform Panel(string name, Transform parent, Color color, float alpha)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, alpha);
            return go.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static Button ButtonWithLabel(string name, Transform parent, Color color, Color textColor, out TMP_Text label)
        {
            var rect = Panel(name, parent, color, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            button.colors = colors;

            var text = Text("Label", rect, name, 22, FontStyles.Bold, textColor);
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform);
            label = text;
            return button;
        }

        private static VerticalLayoutGroup Vertical(GameObject go, float spacing, RectOffset padding)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Image Filled precisa de um sprite; usa o sprite interno da UI padrão.</summary>
        private static Sprite WhiteSprite() =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        private static void SetRef(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Capi] Campo '{field}' não encontrado em {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Capi] Campo '{field}' não encontrado em {target.GetType().Name}.");
                return;
            }
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
