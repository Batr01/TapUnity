#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TapBrawl.EditorTools
{
    /// <summary>
    /// Одноразовая генерация: мультяшная текстура «пух», материал URP Particles/Unlit и префаб с Particle System.
    /// Меню: <b>Tap → VFX → Создать мультяшный дым (текстура + материал + префаб)</b>
    /// </summary>
    public static class CartoonSmokeVfxAuthoring
    {
        private const string TexturePath = "Assets/Art/VFX/CartoonSmoke/CartoonSmokePuff.png";
        private const string MaterialPath = "Assets/Art/VFX/CartoonSmoke/CartoonSmokePuff_Mat.mat";
        private const string PrefabPath = "Assets/Prefabs/VFX/CartoonSmokeCloud.prefab";

        [MenuItem("Tap/VFX/Создать мультяшный дым (текстура + материал + префаб)", priority = 500)]
        public static void CreateCartoonSmokeAssets()
        {
            EnsureDir("Assets/Art/VFX/CartoonSmoke");
            EnsureDir("Assets/Prefabs/VFX");

            if (File.Exists(MaterialPath))
                AssetDatabase.DeleteAsset(MaterialPath);
            if (File.Exists(PrefabPath))
                AssetDatabase.DeleteAsset(PrefabPath);

            var tex = BuildCartoonPuffTexture(256);
            var png = tex.EncodeToPNG();
            File.WriteAllBytes(TexturePath, png);
            Object.DestroyImmediate(tex);
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 512;
                importer.SaveAndReimport();
            }

            var textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (textureAsset == null)
            {
                Debug.LogError("[CartoonSmoke] Не удалось импортировать текстуру.");
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Unlit");
            if (shader == null)
            {
                Debug.LogError("[CartoonSmoke] Не найден шейдер URP Particles/Unlit.");
                return;
            }

            var mat = new Material(shader)
            {
                name = "CartoonSmokePuff_Mat"
            };
            mat.SetTexture("_BaseMap", textureAsset);
            mat.SetColor("_BaseColor", Color.white);
            mat.renderQueue = 3000;

            AssetDatabase.CreateAsset(mat, MaterialPath);
            AssetDatabase.SaveAssets();

            var root = new GameObject("CartoonSmokeCloud");
            var ps = root.AddComponent<ParticleSystem>();
            ConfigureCartoonParticleSystem(ps);

            var rend = root.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = mat;
            rend.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            rend.maxParticleSize = 1f;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.Refresh();
            Debug.Log($"[CartoonSmoke] Готово:\n- {TexturePath}\n- {MaterialPath}\n- {PrefabPath}\nПеретащите префаб дочерним к <b>SmokeVeilRoot</b> / <b>SmokeParticlesHost</b> или замените существующий Particle System.");
        }

        private static void EnsureDir(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(dir))
                return;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>Мягкие «метаболлы» — читаемый мультяшный клуб дыма с альфой.</summary>
        private static Texture2D BuildCartoonPuffTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var uv = new Vector2(x + 0.5f, y + 0.5f) / size;
                    var c = SampleCartoonPuff(uv);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(true, false);
            return tex;
        }

        private static float MetaballBlob(Vector2 p, Vector2 center, float sharp)
        {
            var d = p - center;
            return Mathf.Exp(-d.sqrMagnitude * sharp);
        }

        private static Color SampleCartoonPuff(Vector2 uv)
        {
            // Мягкое «облако тумана»: широкий центр, плавный спад альфы по краю.
            var b =
                MetaballBlob(uv, new Vector2(0.5f, 0.5f), 9.5f) * 1.05f +
                MetaballBlob(uv, new Vector2(0.38f, 0.58f), 11f) * 0.45f +
                MetaballBlob(uv, new Vector2(0.62f, 0.44f), 10f) * 0.42f;

            var a = Mathf.Clamp01(b);
            var rim = Mathf.Pow(a, 0.42f);
            var col = Color.Lerp(new Color(0.5f, 0.54f, 0.62f), new Color(0.74f, 0.78f, 0.86f), rim);
            col.a = Mathf.Clamp01(Mathf.Pow(a, 0.85f) * 0.78f);
            return col;
        }

        private static void ConfigureCartoonParticleSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.duration = 6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.2f, 7.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.012f, 0.06f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI * 0.35f, Mathf.PI * 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.gravityModifier = 0f;
            main.maxParticles = 220;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.62f, 0.66f, 0.74f, 0.22f),
                new Color(0.48f, 0.52f, 0.62f, 0.38f));

            var emission = ps.emission;
            emission.rateOverTime = 52f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.42f;
            shape.radiusThickness = 0.92f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.045f, 0.045f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.018f, 0.028f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.separateAxes = false;
            noise.strength = new ParticleSystem.MinMaxCurve(0.22f, 0.38f);
            noise.frequency = 0.42f;
            noise.scrollSpeed = 0.07f;
            noise.damping = true;
            noise.octaveCount = 2;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.78f, 0.82f, 0.9f), 0f),
                    new GradientColorKey(new Color(0.58f, 0.62f, 0.72f), 0.45f),
                    new GradientColorKey(new Color(0.48f, 0.52f, 0.62f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.08f, 0.06f),
                    new GradientAlphaKey(0.32f, 0.28f),
                    new GradientAlphaKey(0.26f, 0.55f),
                    new GradientAlphaKey(0.14f, 0.78f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.12f, 0f, 2.4f),
                new Keyframe(0.22f, 0.95f, 0.2f, 0.05f),
                new Keyframe(0.52f, 1f, 0f, 0f),
                new Keyframe(0.82f, 0.78f, -0.35f, -0.35f),
                new Keyframe(1f, 0.2f, -0.6f, 0f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-10f, 10f);
        }
    }
}
#endif
