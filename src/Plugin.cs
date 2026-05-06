using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using HarmonyLib;
using UnityEngine;
using xiaoye97;

namespace ProliferatorMk4
{
    [BepInPlugin(ModGuid, ModName, Version)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool")]
    [CommonAPISubmoduleDependency(nameof(LocalizationModule), nameof(ProtoRegistry))]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.komonad.dsp.proliferatormk4";
        public const string ModName = "Proliferator Mk.IV";
        public const string Version = "0.1.2";

        private const int DefaultItemProliferatorMk4 = 9441;
        private const int DefaultRecipeProliferatorMk4 = 9441;
        private const int DefaultTechProliferatorMk4 = 1998;
        private const int DefaultSprayCoaterGridIndex = 2207;
        private const int HiddenGridIndex = 0;
        private const int MaxGridPage = 8;
        private const int MaxGridRow = 7;
        private const int MaxGridColumn = 14;

        private const int ItemProliferatorMk3 = 1143;
        private const int ItemStrangeMatter = 1127;
        private const int ItemElectromagneticMatrix = 6001;
        private const int ItemEnergyMatrix = 6002;
        private const int ItemStructureMatrix = 6003;
        private const int ItemInformationMatrix = 6004;
        private const int ItemGravityMatrix = 6005;
        private const int ItemUniverseMatrix = 6006;
        private const int ItemSprayCoater = 2313;
        private const int ModelSprayCoater = 120;
        private const int DefaultModelSprayCoaterMk4IdleVisual = 720;
        private const int DefaultModelSprayCoaterMk4ActiveVisual = 721;
        private const int RecipeProliferatorMk3 = 108;
        private const int RecipeSprayCoater = 109;
        private const int TechProliferatorMk3 = 1153;
        private const int TechSubmicroscopicQuantumEntanglement = 1305;

        private const string TextProliferatorMk4Name = "ProliferatorMk4.Item.Name";
        private const string TextProliferatorMk4Description = "ProliferatorMk4.Item.Description";
        private const string TextProliferatorMk4RecipeDescription = "ProliferatorMk4.Recipe.Description";
        private const string TextTechName = "ProliferatorMk4.Tech.HighDimensionalProliferation.Name";
        private const string TextTechDescription = "ProliferatorMk4.Tech.Description";
        private const string TextTechConclusion = "ProliferatorMk4.Tech.Conclusion";
        private const string IconProliferatorMk4 = "Assets/texpack/proliferator-mk4";
        private const string IconProliferatorMk4Tech = "Assets/texpack/proliferator-mk4-tech";

        private const int Mk4SpraycoaterVisualAbility = 4;

        private static readonly Color Mk4SprayTankColor = new Color(0.30f, 0.07f, 0.46f, 0.88f);
        private static readonly Color Mk4SprayBgColor = new Color(0.08f, 0.03f, 0.14f, 0.76f);
        private static readonly Color Mk4SprayBrightColor = new Color(0.52f, 0.18f, 0.74f, 1f);
        private static readonly Color Mk4ModelBodyColor = new Color(0.36f, 0.08f, 0.52f, 1f);
        private static readonly Color Mk4ModelWaterColor = new Color(0.42f, 0.10f, 0.60f, 0.86f);
        private static readonly Color Mk4ModelGlowColor = new Color(0.72f, 0.24f, 0.95f, 1f);
        private static readonly Color Mk4ModelGlowOffColor = new Color(0f, 0f, 0f, 0f);
        private static readonly HashSet<long> Mk4SpraycoaterEntities = new HashSet<long>();
        private static readonly object Mk4SpraycoaterEntitiesLock = new object();
        private static readonly Dictionary<long, int> PendingSpraycoaterVisualModelUpdates = new Dictionary<long, int>();
        private static readonly object PendingSpraycoaterVisualModelUpdatesLock = new object();
        private static readonly HashSet<int> ReservedModelIds = new HashSet<int>();
        private static readonly FieldInfo ModelProtoColliderPathField = AccessTools.Field(typeof(ModelProto), "_colliderPath");
        private static readonly FieldInfo ModelProtoRuinPathField = AccessTools.Field(typeof(ModelProto), "<_ruinPath>k__BackingField");
        private static readonly FieldInfo ModelProtoWreckagePathField = AccessTools.Field(typeof(ModelProto), "<_wreckagePath>k__BackingField");
        private static readonly FieldInfo ModelProtoRuinOriginModelIndexField = AccessTools.Field(typeof(ModelProto), "<_ruinOriginModelIndex>k__BackingField");
        private static readonly MethodInfo GameMainCreateGPUInstancingMethod = AccessTools.Method(typeof(GameMain), "CreateGPUInstancing");
        private static readonly MethodInfo GameMainCreateBPGPUInstancingMethod = AccessTools.Method(typeof(GameMain), "CreateBPGPUInstancing");
        private static readonly MethodInfo GameMainCreateStarmapGPUInstancingMethod = AccessTools.Method(typeof(GameMain), "CreateStarmapGPUInstancing");

        private static int _itemProliferatorMk4 = DefaultItemProliferatorMk4;
        private static int _modelSprayCoaterMk4IdleVisual = DefaultModelSprayCoaterMk4IdleVisual;
        private static int _modelSprayCoaterMk4ActiveVisual = DefaultModelSprayCoaterMk4ActiveVisual;
        private static int _techProliferatorMk3;
        private static int _techProliferatorMk4 = DefaultTechProliferatorMk4;
        private static bool _visualModelSwitchDisabled;

        private void Awake()
        {
            Logger.LogInfo($"{ModName} loaded.");
            RegisterLocalizations();
            LDBTool.PreAddDataAction += RegisterProtos;
            LDBTool.PostAddDataAction += PostAddData;
            new Harmony(ModGuid).PatchAll();
        }

        private void OnDestroy()
        {
            LDBTool.PreAddDataAction -= RegisterProtos;
            LDBTool.PostAddDataAction -= PostAddData;
        }

        private void RegisterProtos()
        {
            ReservedModelIds.Clear();

            ItemProto item = CreateProliferatorMk4Item();
            LDBTool.PreAddProto(item);
            _itemProliferatorMk4 = item.ID;
            RegisterMk4BeltItemColor(item.ID);

            ModelProto mk4SprayCoaterIdleVisual = CreateMk4SprayCoaterVisualModel(DefaultModelSprayCoaterMk4IdleVisual, false);
            if (mk4SprayCoaterIdleVisual != null)
            {
                LDBTool.PreAddProto(mk4SprayCoaterIdleVisual);
                _modelSprayCoaterMk4IdleVisual = mk4SprayCoaterIdleVisual.ID;
            }
            else
            {
                _modelSprayCoaterMk4IdleVisual = 0;
            }

            ModelProto mk4SprayCoaterActiveVisual = CreateMk4SprayCoaterVisualModel(DefaultModelSprayCoaterMk4ActiveVisual, true);
            if (mk4SprayCoaterActiveVisual != null)
            {
                LDBTool.PreAddProto(mk4SprayCoaterActiveVisual);
                _modelSprayCoaterMk4ActiveVisual = mk4SprayCoaterActiveVisual.ID;
            }
            else
            {
                _modelSprayCoaterMk4ActiveVisual = 0;
            }

            RecipeProto recipe = CreateProliferatorMk4Recipe(item.ID);
            LDBTool.PreAddProto(recipe);

            if (item.GridIndex == HiddenGridIndex || recipe.GridIndex == HiddenGridIndex)
            {
                Logger.LogWarning(
                    $"Could not find a safe spray-coater-page grid slot for Proliferator Mk.IV. itemGrid={item.GridIndex}, recipeGrid={recipe.GridIndex}");
            }

            TechProto tech = CreateProliferatorMk4Tech(recipe.ID);
            LDBTool.PreAddProto(tech);
            _techProliferatorMk4 = tech.ID;

            Logger.LogInfo($"Registered Proliferator Mk.IV protos: item={item.ID}, recipe={recipe.ID}, tech={tech.ID}, itemGrid={item.GridIndex}, recipeGrid={recipe.GridIndex}, sprayCoaterIdleVisualModel={_modelSprayCoaterMk4IdleVisual}, sprayCoaterActiveVisualModel={_modelSprayCoaterMk4ActiveVisual}, prereqs={string.Join(",", tech.PreTechs)}, mk3Tech={_techProliferatorMk3}");
        }

        private static void RegisterLocalizations()
        {
            LocalizationModule.RegisterTranslation(
                TextProliferatorMk4Name,
                "Proliferator Mk.IV",
                "增产剂 Mk.IV",
                "");
            LocalizationModule.RegisterTranslation(
                TextProliferatorMk4Description,
                "Compared to Proliferator Mk.III, Proliferator Mk.IV uses the negative gravitational pressure of Strange Matter to constrain its active ingredients at a higher density. When cargo sprayed with Proliferator Mk.IV is used as raw materials to produce the next-level products, the extra products or production speedup effect can be further improved. However, power consumption will increase, and it requires a more sufficient supply of raw materials.",
                "在增产剂MK.III的基础上，利用奇异物质的引力负压特性对有效成分进行更高密度的约束。喷涂了增产剂MK.IV后的货物作为原材料生产下一级产物时，额外产出或者加速生产产物的效果能够进一步提升，同时整个生产过程的耗电量也会进一步提高，对原料供应提出了更高的要求。",
                "");
            LocalizationModule.RegisterTranslation(
                TextProliferatorMk4RecipeDescription,
                "Uses Strange Matter's negative gravitational pressure to further compress the active ingredients of Proliferator Mk.III.",
                "利用奇异物质的引力负压，对增产剂MK.III的有效成分进行进一步压缩。",
                "");
            LocalizationModule.RegisterTranslation(
                TextTechName,
                "Proliferator Mk.IV",
                "增产剂 Mk.IV",
                "");
            LocalizationModule.RegisterTranslation(
                TextTechDescription,
                "Strange Matter carries negative gravitational pressure that can warp the space around it. By using this property to constrain the active ingredients of Proliferators at a higher density, Proliferator Mk.IV has been invented. It provides a more significant extra products or production speedup effect, though it also puts higher requirements on production line design.",
                "奇异物质本身具有的引力负压能够使周围空间发生翘曲。利用这一特性对增产剂的有效成分进行更高密度的约束后，增产剂MK.IV诞生了。它的增产效果更明显，当然对产线设计也提出了更高的要求。",
                "");
            LocalizationModule.RegisterTranslation(
                TextTechConclusion,
                "You've unlocked the \\zcj4-;. Using the Proliferator Mk.IV to coat cargo before using it as raw materials for next-level product manufacturing will further increase the yield of extra products or the production speedup effect. This also means that a more sufficient supply of raw materials and cargo overflow prevention are required.",
                "你解锁了\\zcj4-;，喷涂后的货物作为原材料生产下一级产物时，能够进一步增加产物的额外产出或者加速生产的效率，这也意味着需要保证更充足的原料供应以及防止货物堆叠。",
                "");
        }

        private void PostAddData()
        {
            RefreshProtoLinks();
            RefreshPrefabDescCaches();
            AddMk4ToSprayCoater();
        }

        private static ItemProto CreateProliferatorMk4Item()
        {
            return new ItemProto
            {
                ID = DefaultItemProliferatorMk4,
                Name = TextProliferatorMk4Name,
                Description = TextProliferatorMk4Description,
                IconPath = IconProliferatorMk4,
                IconTag = "zcj4",
                GridIndex = ResolveFreeItemGridIndex(LDB.items?.dataArray),
                StackSize = 200,
                Type = EItemType.Material,
                DescFields = new[] { 29, 41, 42, 43, 1, 40 },
                FuelType = 0,
                HeatValue = 0,
                ReactorInc = 0f,
                IsFluid = false,
                Productive = true,
                SubID = 0,
                MiningFrom = string.Empty,
                ProduceFrom = string.Empty,
                Grade = 0,
                Upgrades = Array.Empty<int>(),
                IsEntity = false,
                CanBuild = false,
                BuildInGas = false,
                ModelIndex = 0,
                ModelCount = 0,
                HpMax = 120,
                Ability = 8,
                Potential = 0,
                BuildIndex = 0,
                BuildMode = 0,
                UnlockKey = 0,
                MechaMaterialID = 0,
                AmmoType = EAmmoType.None,
                BombType = EBombType.None,
                CraftType = 0,
                DropRate = 0f,
                EnemyDropLevel = 0,
                EnemyDropRange = Vector2.zero,
                EnemyDropCount = 0f,
                EnemyDropMask = 0,
                EnemyDropMaskRatio = 0f
            };
        }

        private static RecipeProto CreateProliferatorMk4Recipe(int itemProliferatorMk4)
        {
            return new RecipeProto
            {
                ID = DefaultRecipeProliferatorMk4,
                Name = TextProliferatorMk4Name,
                Description = TextProliferatorMk4RecipeDescription,
                IconPath = IconProliferatorMk4,
                IconTag = "zcj4",
                Type = ERecipeType.Assemble,
                GridIndex = ResolveFreeRecipeGridIndex(LDB.recipes?.dataArray),
                TimeSpend = 240,
                Items = new[] { ItemProliferatorMk3, ItemStrangeMatter },
                ItemCounts = new[] { 2, 1 },
                Results = new[] { itemProliferatorMk4 },
                ResultCounts = new[] { 1 },
                Explicit = false,
                Handcraft = false,
                NonProductive = false
            };
        }

        private static TechProto CreateProliferatorMk4Tech(int recipeProliferatorMk4)
        {
            int techProliferatorMk3 = ResolveMk3ProliferatorTechId();
            _techProliferatorMk3 = techProliferatorMk3;
            int[] preTechs = techProliferatorMk3 > 0 && techProliferatorMk3 != TechSubmicroscopicQuantumEntanglement
                ? new[] { TechSubmicroscopicQuantumEntanglement, techProliferatorMk3 }
                : new[] { TechSubmicroscopicQuantumEntanglement };

            return new TechProto
            {
                ID = DefaultTechProliferatorMk4,
                Name = TextTechName,
                Desc = TextTechDescription,
                Conclusion = TextTechConclusion,
                IconPath = IconProliferatorMk4Tech,
                IconTag = "zcj4",
                IsHiddenTech = false,
                PreItem = Array.Empty<int>(),
                Position = new Vector2(57f, -11f),
                PreTechs = preTechs,
                PreTechsImplicit = Array.Empty<int>(),
                Items = new[]
                {
                    ItemElectromagneticMatrix,
                    ItemEnergyMatrix,
                    ItemStructureMatrix,
                    ItemInformationMatrix,
                    ItemGravityMatrix
                },
                ItemPoints = new[] { 1, 1, 1, 1, 1 },
                HashNeeded = 3600000L,
                UnlockRecipes = new[] { recipeProliferatorMk4 },
                UnlockFunctions = Array.Empty<int>(),
                UnlockValues = Array.Empty<double>(),
                Published = true,
                Level = 0,
                MaxLevel = 0,
                LevelCoef1 = 0,
                LevelCoef2 = 0,
                IsLabTech = true,
                PreTechsMax = false,
                AddItems = Array.Empty<int>(),
                AddItemCounts = Array.Empty<int>(),
                PropertyOverrideItems = Array.Empty<int>(),
                PropertyItemCounts = Array.Empty<int>()
            };
        }

        private static int ResolveMk3ProliferatorTechId()
        {
            RecipeProto mk3Recipe = LDB.recipes.Select(RecipeProliferatorMk3);
            if (mk3Recipe?.preTech != null)
            {
                return mk3Recipe.preTech.ID;
            }

            TechProto mk3Tech = LDB.techs.dataArray.FirstOrDefault(proto =>
                proto != null &&
                proto.UnlockRecipes != null &&
                proto.UnlockRecipes.Contains(RecipeProliferatorMk3));

            return mk3Tech?.ID ?? TechProliferatorMk3;
        }

        private static int ResolveFreeItemGridIndex(ItemProto[] dataArray)
        {
            ItemProto[] protos = dataArray?.Where(proto => proto != null).ToArray() ?? Array.Empty<ItemProto>();
            int anchorGridIndex = protos.FirstOrDefault(proto => proto.ID == ItemSprayCoater)?.GridIndex
                ?? DefaultSprayCoaterGridIndex;
            return ResolveFreeGridIndexOnSamePage(protos.Select(proto => proto.GridIndex), anchorGridIndex);
        }

        private static int ResolveFreeRecipeGridIndex(RecipeProto[] dataArray)
        {
            RecipeProto[] protos = dataArray?.Where(proto => proto != null).ToArray() ?? Array.Empty<RecipeProto>();
            int anchorGridIndex = protos.FirstOrDefault(proto => proto.ID == RecipeSprayCoater)?.GridIndex
                ?? DefaultSprayCoaterGridIndex;
            return ResolveFreeGridIndexOnSamePage(protos.Select(proto => proto.GridIndex), anchorGridIndex);
        }

        private static int ResolveFreeGridIndexOnSamePage(IEnumerable<int> occupiedGridIndices, int anchorGridIndex)
        {
            HashSet<int> occupied = new HashSet<int>((occupiedGridIndices ?? Enumerable.Empty<int>()).Where(IsValidGridIndex));
            List<int> candidates = new List<int>();

            if (!TryDecodeGridIndex(anchorGridIndex, out int page, out int row, out int column) &&
                !TryDecodeGridIndex(DefaultSprayCoaterGridIndex, out page, out row, out column))
            {
                return HiddenGridIndex;
            }

            for (int candidateColumn = column + 1; candidateColumn <= MaxGridColumn; candidateColumn++)
            {
                AddGridCandidate(candidates, ToGridIndex(page, row, candidateColumn));
            }

            for (int candidateColumn = column - 1; candidateColumn >= 1; candidateColumn--)
            {
                AddGridCandidate(candidates, ToGridIndex(page, row, candidateColumn));
            }

            for (int candidateRow = row + 1; candidateRow <= MaxGridRow; candidateRow++)
            {
                AddGridRowCandidates(candidates, page, candidateRow);
            }

            for (int candidateRow = row - 1; candidateRow >= 1; candidateRow--)
            {
                AddGridRowCandidates(candidates, page, candidateRow);
            }

            foreach (int gridIndex in candidates)
            {
                if (!occupied.Contains(gridIndex))
                {
                    return gridIndex;
                }
            }

            return HiddenGridIndex;
        }

        private static void AddGridRowCandidates(List<int> candidates, int page, int row)
        {
            for (int column = 1; column <= MaxGridColumn; column++)
            {
                AddGridCandidate(candidates, ToGridIndex(page, row, column));
            }
        }

        private static void AddGridCandidate(List<int> candidates, int gridIndex)
        {
            if (IsValidGridIndex(gridIndex) && !candidates.Contains(gridIndex))
            {
                candidates.Add(gridIndex);
            }
        }

        private static bool IsValidGridIndex(int gridIndex)
        {
            return TryDecodeGridIndex(gridIndex, out _, out _, out _);
        }

        private static bool TryDecodeGridIndex(int gridIndex, out int page, out int row, out int column)
        {
            page = gridIndex / 1000;
            row = gridIndex % 1000 / 100;
            column = gridIndex % 100;
            return page >= 1 && page <= MaxGridPage
                && row >= 1 && row <= MaxGridRow
                && column >= 1 && column <= MaxGridColumn;
        }

        private static int ToGridIndex(int page, int row, int column)
        {
            return page * 1000 + row * 100 + column;
        }

        private static void RegisterMk4BeltItemColor(int itemId)
        {
            Dictionary<int, IconToolNew.IconDesc> itemIconDescs =
                (Dictionary<int, IconToolNew.IconDesc>)AccessTools
                    .Field(typeof(ProtoRegistry), "itemIconDescs")
                    .GetValue(null);

            IconToolNew.IconDesc iconDesc = new IconToolNew.IconDesc
            {
                faceColor = new Color(0.56f, 0.17f, 0.70f, 1f),
                sideColor = new Color(0.56f, 0.17f, 0.70f, 1f),
                faceEmission = new Color(0.18f, 0.035f, 0.25f, 1f),
                sideEmission = new Color(0.18f, 0.035f, 0.25f, 1f),
                iconEmission = Color.clear,
                metallic = 0.92f,
                smoothness = 0.62f,
                solidAlpha = 1f,
                iconAlpha = 0f
            };

            itemIconDescs[itemId] = iconDesc;
        }

        private static ModelProto CreateMk4SprayCoaterVisualModel(int preferredModelId, bool activeGlow)
        {
            ModelProto sourceModel = LDB.models.Select(ModelSprayCoater);
            if (sourceModel == null || sourceModel.prefabDesc == null)
            {
                return null;
            }

            int modelId = ResolveFreeModelId(preferredModelId);
            if (modelId <= 0)
            {
                return null;
            }

            PrefabDesc sourceDesc = sourceModel.prefabDesc;
            GameObject prefab = sourceDesc.prefab != null ? sourceDesc.prefab : Resources.Load<GameObject>(sourceModel.PrefabPath);
            GameObject colliderPrefab = sourceDesc.colliderPrefab != null
                ? sourceDesc.colliderPrefab
                : Resources.Load<GameObject>(GetModelProtoColliderPath(sourceModel));

            if (prefab == null)
            {
                return null;
            }

            ModelProto model = CopyModelProto(sourceModel);
            model.ID = modelId;
            model.Name = modelId.ToString();
            model.SID = string.Empty;
            model.sid = string.Empty;

            model.prefabDesc = colliderPrefab == null
                ? new PrefabDesc(modelId, prefab)
                : new PrefabDesc(modelId, prefab, colliderPrefab);

            CopyPrefabDescPublicFields(sourceDesc, model.prefabDesc);
            model.prefabDesc.modelIndex = modelId;
            model.prefabDesc.prefab = prefab;
            model.prefabDesc.colliderPrefab = colliderPrefab;
            model.prefabDesc.incItemId = sourceDesc.incItemId?.ToArray();
            model.prefabDesc.lodMaterials = CloneMaterialMatrix(sourceDesc.lodMaterials);
            model.prefabDesc.lodBlueprintMaterials = CloneMaterialMatrix(sourceDesc.lodBlueprintMaterials);
            ApplyMk4SprayCoaterModelColors(model.prefabDesc, activeGlow);

            return model;
        }

        private static int ResolveFreeModelId(int preferred)
        {
            ModelProto[] models = LDB.models?.dataArray;
            if (models == null || models.Length == 0)
            {
                return preferred;
            }

            HashSet<int> occupied = new HashSet<int>(models.Where(proto => proto != null).Select(proto => proto.ID));
            occupied.UnionWith(ReservedModelIds);
            int maxAllowed = models.Length + 64;
            if (preferred > 0 && preferred <= maxAllowed && !occupied.Contains(preferred))
            {
                ReservedModelIds.Add(preferred);
                return preferred;
            }

            for (int modelId = maxAllowed; modelId > 0; modelId--)
            {
                if (!occupied.Contains(modelId))
                {
                    ReservedModelIds.Add(modelId);
                    return modelId;
                }
            }

            return 0;
        }

        private static ModelProto CopyModelProto(ModelProto source)
        {
            ModelProto model = new ModelProto
            {
                ID = source.ID,
                Name = source.Name,
                SID = source.SID,
                OverrideName = source.OverrideName,
                Order = source.Order,
                ObjectType = source.ObjectType,
                RuinType = source.RuinType,
                RendererType = source.RendererType,
                RotSymmetry = source.RotSymmetry,
                HpMax = source.HpMax,
                HpUpgrade = source.HpUpgrade,
                HpRecover = source.HpRecover,
                RuinId = source.RuinId,
                RuinCount = source.RuinCount,
                RuinLifeTime = source.RuinLifeTime,
                PrefabPath = source.PrefabPath,
                meshBounds = source.meshBounds
            };

            SetModelProtoColliderPath(model, GetModelProtoColliderPath(source));
            CopyModelProtoPrivateRuntimeFields(source, model);
            return model;
        }

        private static string GetModelProtoColliderPath(ModelProto model)
        {
            if (model == null)
            {
                return string.Empty;
            }

            return model.ColliderPath ?? (string)ModelProtoColliderPathField?.GetValue(model) ?? string.Empty;
        }

        private static void SetModelProtoColliderPath(ModelProto model, string colliderPath)
        {
            if (model != null && ModelProtoColliderPathField != null)
            {
                ModelProtoColliderPathField.SetValue(model, colliderPath ?? string.Empty);
            }
        }

        private static void CopyModelProtoPrivateRuntimeFields(ModelProto source, ModelProto target)
        {
            CopyFieldValue(ModelProtoRuinPathField, source, target);
            CopyFieldValue(ModelProtoWreckagePathField, source, target);
            CopyFieldValue(ModelProtoRuinOriginModelIndexField, source, target);
        }

        private static void CopyFieldValue(FieldInfo field, object source, object target)
        {
            if (field != null && source != null && target != null)
            {
                field.SetValue(target, field.GetValue(source));
            }
        }

        private static void CopyPrefabDescPublicFields(PrefabDesc source, PrefabDesc target)
        {
            foreach (FieldInfo field in typeof(PrefabDesc).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                field.SetValue(target, field.GetValue(source));
            }
        }

        private static Material[][] CloneMaterialMatrix(Material[][] source)
        {
            if (source == null)
            {
                return null;
            }

            Material[][] result = new Material[source.Length][];
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null)
                {
                    continue;
                }

                result[i] = new Material[source[i].Length];
                for (int j = 0; j < source[i].Length; j++)
                {
                    result[i][j] = source[i][j] == null ? null : new Material(source[i][j]);
                }
            }

            return result;
        }

        private static void ApplyMk4SprayCoaterModelColors(PrefabDesc prefabDesc, bool activeGlow)
        {
            ApplyMk4SprayCoaterMaterialColors(prefabDesc.lodMaterials, activeGlow);
            ApplyMk4SprayCoaterMaterialColors(prefabDesc.lodBlueprintMaterials, activeGlow);
        }

        private static void ApplyMk4SprayCoaterMaterialColors(Material[][] lodMaterials, bool activeGlow)
        {
            if (lodMaterials == null)
            {
                return;
            }

            foreach (Material[] materials in lodMaterials)
            {
                if (materials == null)
                {
                    continue;
                }

                foreach (Material material in materials)
                {
                    ApplyMk4SprayCoaterMaterialColors(material, activeGlow);
                }
            }
        }

        private static void ApplyMk4SprayCoaterMaterialColors(Material material, bool activeGlow)
        {
            if (material == null)
            {
                return;
            }

            string materialName = material.name == null ? string.Empty : material.name.ToLowerInvariant();
            bool isEffectMaterial = materialName.Contains("effect");
            Color glowColor = activeGlow ? Mk4ModelGlowColor : Mk4ModelGlowOffColor;
            if (material.HasProperty("_Color4"))
            {
                material.SetColor(
                    "_Color4",
                    isEffectMaterial ? glowColor : Mk4ModelWaterColor);
            }

            if (material.HasProperty("_Color2"))
            {
                material.SetColor("_Color2", activeGlow ? Mk4ModelBodyColor : Mk4ModelGlowOffColor);
            }

            if (isEffectMaterial && material.HasProperty("_TintColor"))
            {
                material.SetColor("_TintColor", glowColor);
            }
        }

        private static void RefreshProtoLinks()
        {
            LDB.models.OnAfterDeserialize();
            ModelProto.InitMaxModelIndex();
            ModelProto.InitModelIndices();
            ModelProto.InitModelOrders();
            LDB.items.OnAfterDeserialize();
            LDB.recipes.OnAfterDeserialize();
            LDB.techs.OnAfterDeserialize();

            foreach (TechProto tech in LDB.techs.dataArray)
            {
                if (tech != null)
                {
                    tech.Preload();
                }
            }

            for (int i = 0; i < LDB.items.dataArray.Length; i++)
            {
                ItemProto item = LDB.items.dataArray[i];
                if (item == null)
                {
                    continue;
                }

                item.recipes = null;
                item.rawMats = null;
                item.Preload(i);
            }

            for (int i = 0; i < LDB.recipes.dataArray.Length; i++)
            {
                RecipeProto recipe = LDB.recipes.dataArray[i];
                if (recipe != null)
                {
                    recipe.Preload(i);
                }
            }

            foreach (TechProto tech in LDB.techs.dataArray)
            {
                if (tech == null)
                {
                    continue;
                }

                tech.PreTechsImplicit = (tech.PreTechsImplicit ?? Array.Empty<int>())
                    .Except(tech.PreTechs ?? Array.Empty<int>())
                    .ToArray();
                tech.UnlockRecipes = (tech.UnlockRecipes ?? Array.Empty<int>()).Distinct().ToArray();
                tech.Preload2();
            }
        }

        private static void RefreshPrefabDescCaches()
        {
            PlanetFactory.PrefabDescByModelIndex = null;
            PlanetFactory.InitPrefabDescArray();
            SpaceSector.PrefabDescByModelIndex = null;
            SpaceSector.InitPrefabDescArray();

            if (GameMain.instance == null)
            {
                return;
            }

            InvokeGameMainMethod(GameMainCreateGPUInstancingMethod);
            InvokeGameMainMethod(GameMainCreateBPGPUInstancingMethod);
            InvokeGameMainMethod(GameMainCreateStarmapGPUInstancingMethod);
        }

        private static void InvokeGameMainMethod(MethodInfo method)
        {
            if (method != null)
            {
                method.Invoke(GameMain.instance, null);
            }
        }

        private void AddMk4ToSprayCoater()
        {
            ItemProto sprayCoater = LDB.items.Select(ItemSprayCoater);
            if (sprayCoater == null)
            {
                Logger.LogWarning("Could not find spray coater item proto.");
                return;
            }

            bool patched = false;
            patched |= AddIncItem(sprayCoater.prefabDesc);

            ModelProto model = LDB.models.Select(sprayCoater.ModelIndex);
            if (model != null)
            {
                patched |= AddIncItem(model.prefabDesc);
            }

            if (PlanetFactory.PrefabDescByModelIndex != null &&
                sprayCoater.ModelIndex >= 0 &&
                sprayCoater.ModelIndex < PlanetFactory.PrefabDescByModelIndex.Length)
            {
                patched |= AddIncItem(PlanetFactory.PrefabDescByModelIndex[sprayCoater.ModelIndex]);
            }

            patched |= AddMk4ToSprayCoaterVisualModel(_modelSprayCoaterMk4IdleVisual);
            patched |= AddMk4ToSprayCoaterVisualModel(_modelSprayCoaterMk4ActiveVisual);

            Logger.LogInfo(patched
                ? $"Spray coater now accepts item {_itemProliferatorMk4}."
                : $"Spray coater already accepted item {_itemProliferatorMk4}.");
        }

        private static bool AddMk4ToSprayCoaterVisualModel(int modelIndex)
        {
            if (modelIndex <= 0)
            {
                return false;
            }

            bool patched = false;
            ModelProto visualModel = LDB.models.Select(modelIndex);
            if (visualModel != null)
            {
                patched |= AddIncItem(visualModel.prefabDesc);
            }

            if (PlanetFactory.PrefabDescByModelIndex != null &&
                modelIndex < PlanetFactory.PrefabDescByModelIndex.Length)
            {
                patched |= AddIncItem(PlanetFactory.PrefabDescByModelIndex[modelIndex]);
            }

            return patched;
        }

        private static bool AddIncItem(PrefabDesc prefabDesc)
        {
            if (prefabDesc == null || prefabDesc.incItemId == null)
            {
                return false;
            }

            if (prefabDesc.incItemId.Contains(_itemProliferatorMk4))
            {
                return false;
            }

            prefabDesc.incItemId = prefabDesc.incItemId.Concat(new[] { _itemProliferatorMk4 }).ToArray();
            return true;
        }

        private static bool IsMk4Spraycoater(CargoTraffic traffic, SpraycoaterComponent spraycoater)
        {
            int totalCount = Math.Max(0, spraycoater.incCount) + Math.Max(0, spraycoater.extraIncCount);
            long key = GetSpraycoaterKey(traffic?.factory, spraycoater.entityId);

            lock (Mk4SpraycoaterEntitiesLock)
            {
                if (spraycoater.incItemId == _itemProliferatorMk4)
                {
                    if (totalCount > 0)
                    {
                        Mk4SpraycoaterEntities.Add(key);
                    }

                    return true;
                }

                if (totalCount <= 0 || spraycoater.incItemId != 0)
                {
                    Mk4SpraycoaterEntities.Remove(key);
                    return false;
                }

                return Mk4SpraycoaterEntities.Contains(key);
            }
        }

        private static long GetSpraycoaterKey(PlanetFactory factory, int entityId)
        {
            return ((long)(factory?.index ?? 0) << 32) | (uint)entityId;
        }

        private static void RewriteMk4SpraycoaterVisualState(CargoTraffic traffic, AnimData[] animPool, SpraycoaterComponent spraycoater)
        {
            bool isMk4 = IsMk4Spraycoater(traffic, spraycoater);
            bool visualActive = IsSpraycoaterVisualActive(animPool, spraycoater.entityId);
            QueueSpraycoaterVisualModelUpdate(traffic?.factory, spraycoater.entityId, ResolveSpraycoaterVisualModelIndex(isMk4, visualActive));

            if (!isMk4 ||
                animPool == null ||
                spraycoater.entityId <= 0 ||
                spraycoater.entityId >= animPool.Length)
            {
                return;
            }

            uint state = animPool[spraycoater.entityId].state;
            uint lowState = state % 1000u;
            uint frameState = state - lowState;
            uint powerState = lowState % 10u;
            uint sprayingState = lowState >= 100u ? 100u : 0u;

            animPool[spraycoater.entityId].state =
                frameState +
                powerState +
                sprayingState +
                (uint)(Mk4SpraycoaterVisualAbility * 10);
        }

        private static bool IsSpraycoaterVisualActive(AnimData[] animPool, int entityId)
        {
            if (animPool == null ||
                entityId <= 0 ||
                entityId >= animPool.Length)
            {
                return false;
            }

            uint state = animPool[entityId].state;
            return state / 1000u > 0u || state % 1000u >= 100u;
        }

        private static int ResolveSpraycoaterVisualModelIndex(bool isMk4, bool isSpraying)
        {
            if (!isMk4)
            {
                return ModelSprayCoater;
            }

            if (isSpraying && _modelSprayCoaterMk4ActiveVisual > 0)
            {
                return _modelSprayCoaterMk4ActiveVisual;
            }

            return _modelSprayCoaterMk4IdleVisual > 0
                ? _modelSprayCoaterMk4IdleVisual
                : _modelSprayCoaterMk4ActiveVisual;
        }

        private static void QueueSpraycoaterVisualModelUpdate(PlanetFactory factory, int entityId, int targetModelIndex)
        {
            if (_visualModelSwitchDisabled ||
                targetModelIndex <= 0 ||
                factory == null ||
                entityId <= 0)
            {
                return;
            }

            long key = GetSpraycoaterKey(factory, entityId);
            lock (PendingSpraycoaterVisualModelUpdatesLock)
            {
                PendingSpraycoaterVisualModelUpdates[key] = targetModelIndex;
            }
        }

        private static void ApplyPendingSpraycoaterVisualModelUpdates(FactoryModel factoryModel)
        {
            PlanetFactory factory = factoryModel?.planet?.factory;
            GPUInstancingManager gpuiManager = factoryModel?.gpuiManager;
            if (_visualModelSwitchDisabled ||
                factory?.entityPool == null ||
                gpuiManager == null)
            {
                return;
            }

            List<KeyValuePair<int, int>> updates = null;
            lock (PendingSpraycoaterVisualModelUpdatesLock)
            {
                foreach (KeyValuePair<long, int> pair in PendingSpraycoaterVisualModelUpdates.ToArray())
                {
                    if ((int)(pair.Key >> 32) != factory.index)
                    {
                        continue;
                    }

                    if (updates == null)
                    {
                        updates = new List<KeyValuePair<int, int>>();
                    }

                    updates.Add(new KeyValuePair<int, int>((int)(uint)pair.Key, pair.Value));
                    PendingSpraycoaterVisualModelUpdates.Remove(pair.Key);
                }
            }

            if (updates == null)
            {
                return;
            }

            foreach (KeyValuePair<int, int> update in updates)
            {
                ApplySpraycoaterVisualModel(factory, gpuiManager, update.Key, update.Value);
            }
        }

        private static void ApplySpraycoaterVisualModel(PlanetFactory factory, GPUInstancingManager gpuiManager, int entityId, int targetModelIndex)
        {
            if (factory?.entityPool == null ||
                gpuiManager == null ||
                targetModelIndex <= 0 ||
                entityId <= 0 ||
                entityId >= factory.entityPool.Length)
            {
                return;
            }

            ref EntityData entity = ref factory.entityPool[entityId];
            if (entity.id != entityId)
            {
                return;
            }

            int currentModelIndex = entity.modelIndex;
            if (currentModelIndex == targetModelIndex ||
                !IsSpraycoaterVisualModelIndex(currentModelIndex) ||
                !IsSpraycoaterVisualModelIndex(targetModelIndex))
            {
                return;
            }

            try
            {
                int newModelId = gpuiManager.AddModel(targetModelIndex, entity.id, entity.pos, entity.rot, true);
                if (newModelId <= 0)
                {
                    return;
                }

                if (entity.modelId > 0)
                {
                    gpuiManager.RemoveModel(currentModelIndex, entity.modelId, true);
                }

                entity.modelIndex = (short)targetModelIndex;
                entity.modelId = newModelId;
            }
            catch (Exception ex)
            {
                _visualModelSwitchDisabled = true;
                lock (PendingSpraycoaterVisualModelUpdatesLock)
                {
                    PendingSpraycoaterVisualModelUpdates.Clear();
                }

                Debug.LogWarning($"Proliferator Mk.IV disabled spray coater model switching after renderer error: {ex}");
            }
        }

        private static bool IsSpraycoaterVisualModelIndex(int modelIndex)
        {
            return modelIndex == ModelSprayCoater ||
                   modelIndex == _modelSprayCoaterMk4IdleVisual ||
                   modelIndex == _modelSprayCoaterMk4ActiveVisual;
        }

        private static void RefreshMk4SpraycoaterTank(UISpraycoaterWindow window, SpraycoaterComponent spraycoater)
        {
            int totalCount = Math.Max(0, spraycoater.incCount) + Math.Max(0, spraycoater.extraIncCount);
            int capacity = Math.Max(1, spraycoater.incCapacity);
            float fillRatio = Mathf.Clamp01(totalCount / (float)capacity);
            float outlineFillRatio = fillRatio > 0f && fillRatio < 0.01f ? 0.01f : fillRatio;

            ItemProto item = LDB.items.Select(_itemProliferatorMk4);
            if (item != null)
            {
                window.tankFillIconImage.sprite = item.iconSprite;
            }

            window.tankFillIconImage.enabled = totalCount > 0;
            window.tankFillIconImage.color = Color.white;
            window.tankFillBgImage.color = Mk4SprayBgColor;
            window.tankFillOutlineImage.fillAmount = outlineFillRatio;
            window.tankFillOutlineImage.color = Mk4SprayBrightColor;
            window.tankFillMaskImage.enabled = totalCount > 0;
            window.tankFillMaskImage.color = Mk4SprayTankColor;
            window.tankFillRectMask.anchoredPosition = new Vector2(0f, -158f + Mathf.Ceil(159f * fillRatio));
            window.tankCountText.text = string.Concat(Localization.Translate("增产剂储量提示"), totalCount.ToString());
            window.tankCountText.color = Mk4SprayBrightColor;
            window.tankBtn.tips.itemId = totalCount > 0 ? _itemProliferatorMk4 : 0;

            SetEffectText(window.incInfoText1, "喷涂增产效果", Cargo.incTable, spraycoater.incAbility);
            SetEffectText(window.incInfoText2, "喷涂加速效果", Cargo.accTable, spraycoater.incAbility);
            SetEffectText(window.incInfoText3, "额外电力消耗", Cargo.powerTable, spraycoater.incAbility);
        }

        private static void SetEffectText(UnityEngine.UI.Text text, string labelKey, int[] table, int ability)
        {
            int tableValue = table != null && ability >= 0 && ability < table.Length ? table[ability] : 0;
            text.text = string.Concat(
                Localization.Translate(labelKey),
                ": ",
                (tableValue * 0.1f).ToString("0.0"),
                "%");
            text.color = Mk4SprayBrightColor;
        }

        private static void EnsureMk4TechState(GameHistoryData history)
        {
            if (history == null || history.techStates == null || history.techStates.ContainsKey(_techProliferatorMk4))
            {
                return;
            }

            TechProto tech = LDB.techs.Select(_techProliferatorMk4);
            if (tech == null)
            {
                return;
            }

            int universeMatrixPoint = 0;
            if (tech.Items != null &&
                tech.ItemPoints != null &&
                tech.Items.Length > 0 &&
                tech.ItemPoints.Length > 0 &&
                tech.Items[0] == ItemUniverseMatrix)
            {
                universeMatrixPoint = tech.ItemPoints[0];
            }

            history.techStates.Add(
                _techProliferatorMk4,
                new TechState(false, tech.Level, tech.MaxLevel, 0L, tech.GetHashNeeded(tech.Level), universeMatrixPoint));
        }

        [HarmonyPatch]
        private static class GameDataPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(GameData), nameof(GameData.SetForNewGame))]
            private static void SetForNewGamePostfix(GameData __instance)
            {
                EnsureMk4TechState(__instance?.history);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GameData), nameof(GameData.Import))]
            private static void ImportPostfix(GameData __instance)
            {
                EnsureMk4TechState(__instance?.history);
            }
        }

        [HarmonyPatch]
        private static class SpraycoaterWindowPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(UISpraycoaterWindow), nameof(UISpraycoaterWindow.RefreshSpraycoaterWindow))]
            private static void RefreshSpraycoaterWindowPostfix(UISpraycoaterWindow __instance, SpraycoaterComponent spraycoater)
            {
                if (!IsMk4Spraycoater(__instance.traffic, spraycoater))
                {
                    return;
                }

                RefreshMk4SpraycoaterTank(__instance, spraycoater);
            }
        }

        [HarmonyPatch]
        private static class SpraycoaterComponentPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SpraycoaterComponent), nameof(SpraycoaterComponent.InternalUpdate))]
            private static void InternalUpdatePostfix(ref SpraycoaterComponent __instance, CargoTraffic _traffic, AnimData[] _animPool)
            {
                RewriteMk4SpraycoaterVisualState(_traffic, _animPool, __instance);
            }
        }

        [HarmonyPatch]
        private static class FactoryModelPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(FactoryModel), nameof(FactoryModel.LateUpdate))]
            private static void LateUpdatePrefix(FactoryModel __instance)
            {
                ApplyPendingSpraycoaterVisualModelUpdates(__instance);
            }
        }
    }
}
