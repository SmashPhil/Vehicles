﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.Defs;

namespace Vehicles
{
	public class VehicleDef : ThingDef
	{
		[PostToSettings]
		internal bool enabled = true;

		[PostToSettings(Label = "VehicleNameable", Translate = true, Tooltip = "VehicleNameableTooltip", UISettingsType = UISettingsType.Checkbox)]
		public bool nameable = false;
		[DisableSetting]
		[PostToSettings(Label = "VehicleRaidsEnabled", Translate = true, Tooltip = "VehicleRaidsEnabledTooltip", UISettingsType = UISettingsType.Checkbox)]
		public bool raidersCanUse = true;

		public float armor;
		[PostToSettings(Label = "VehicleBaseSpeed", Translate = true, Tooltip ="VehicleBaseSpeedTooltip", UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 8, RoundDecimalPlaces = 2, Increment = 0.25f)]
		public float speed;

		[PostToSettings(Label = "VehicleBaseCargo", Translate = true, Tooltip ="VehicleBaseCargoTooltip", UISettingsType = UISettingsType.IntegerBox)]
		public float cargoCapacity;

		[PostToSettings(Label = "VehicleTicksBetweenRepair", Translate = true, Tooltip = "VehicleTicksBetweenRepairTooltip", UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 1, MaxValue = 20f)]
		public float repairRate = 1;

		[PostToSettings(Label = "VehicleCombatPower", Translate = true, Tooltip = "VehicleCombatPowerTooltip", UISettingsType = UISettingsType.FloatBox)]
		[NumericBoxValues(MinValue = 0, MaxValue = float.MaxValue)]
		public float combatPower = 0;

		[PostToSettings(Label = "VehicleMovementPermissions", Translate = true, UISettingsType = UISettingsType.SliderEnum)]
		public VehiclePermissions vehicleMovementPermissions = VehiclePermissions.DriverNeeded;

		public VehicleCategory vehicleCategory = VehicleCategory.Misc;
		public TechLevel vehicleTech = TechLevel.Industrial;
		public VehicleType vehicleType = VehicleType.Land;

		[PostToSettings(Label = "VehicleNavigationType", Translate = true, UISettingsType = UISettingsType.SliderEnum)]
		public NavigationCategory defaultNavigation = NavigationCategory.Opportunistic;

		public VehicleBuildDef buildDef;
		public new GraphicDataRGB graphicData;

		[PostToSettings(Label = "VehicleProperties", Translate = true, ParentHolder = true)]
		public VehicleProperties properties;
		
		public VehicleDrawProperties drawProperties;

		public List<VehicleComponentProperties> components;

		private readonly SelfOrderingList<CompProperties> cachedComps = new SelfOrderingList<CompProperties>();
		private Texture2D resolvedLoadCargoTexture;
		private Texture2D resolvedCancelCargoTexture;

		public PawnKindDef VehicleKindDef { get; internal set; }

		public Texture2D LoadCargoIcon
		{
			get
			{
				if (resolvedLoadCargoTexture is null)
				{
					resolvedLoadCargoTexture = ContentFinder<Texture2D>.Get(drawProperties.loadCargoTexPath, false) ?? VehicleTex.PackCargoIcon[(uint)vehicleType];
				}
				return resolvedLoadCargoTexture;
			}
		}

		public Texture2D CancelCargoIcon
		{
			get
			{
				if (resolvedCancelCargoTexture is null)
				{
					resolvedCancelCargoTexture = ContentFinder<Texture2D>.Get(drawProperties.cancelCargoTexPath, false) ?? VehicleTex.CancelPackCargoIcon[(uint)vehicleType];
				}
				return resolvedCancelCargoTexture;
			}
		}

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			if (!components.NullOrEmpty())
			{
				components.OrderBy(c => c.hitbox.side == VehicleComponentPosition.BodyNoOverlap).ForEach(c => c.ResolveReferences(this));
				properties.roles.OrderBy(c => c.hitbox.side == VehicleComponentPosition.BodyNoOverlap).ForEach(c => c.hitbox.Initialize(this));
			}
			if (properties.overweightSpeedCurve is null)
			{
				properties.overweightSpeedCurve = new SimpleCurve()
				{
					new CurvePoint(0, 1),
					new CurvePoint(0.65f, 1),
					new CurvePoint(0.85f, 0.9f),
					new CurvePoint(1.05f, 0.35f),
					new CurvePoint(1.25f, 0)
				};
			}
			if (VehicleMod.settings.vehicles.defaultMasks.EnumerableNullOrEmpty())
			{
				VehicleMod.settings.vehicles.defaultMasks = new Dictionary<string, string>();
			}
			if (!VehicleMod.settings.vehicles.defaultMasks.ContainsKey(defName))
			{
				VehicleMod.settings.vehicles.defaultMasks.Add(defName, "Default");
			}
			properties.customBiomeCosts ??= new Dictionary<BiomeDef, float>();
			properties.customHillinessCosts ??= new Dictionary<Hilliness, float>();
			properties.customRoadCosts ??= new Dictionary<RoadDef, float>();
			properties.customTerrainCosts ??= new Dictionary<TerrainDef, int>();
			properties.customThingCosts ??= new Dictionary<ThingDef, int>();

			if (vehicleType == VehicleType.Sea)
			{
				if (!properties.customBiomeCosts.TryGetValue(BiomeDefOf.Ocean, out _))
				{
					properties.customBiomeCosts.Add(BiomeDefOf.Ocean, 1);
				}
				properties.customBiomeCosts[BiomeDefOf.Ocean] = 1;
				if (!properties.customBiomeCosts.TryGetValue(BiomeDefOf.Lake, out _))
				{
					properties.customBiomeCosts.Add(BiomeDefOf.Lake, 1);
				}
				properties.customBiomeCosts[BiomeDefOf.Lake] = 1;
			}

			if (!comps.NullOrEmpty())
			{
				cachedComps.AddRange(comps);
			}
		}

		public override void PostLoad()
		{
			base.graphicData = graphicData;
			base.PostLoad();
		}

		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string error in base.ConfigErrors())
			{
				yield return error;
			}
			if (graphicData is null)
			{
				yield return "<field>graphicData</field> must be specified in order to properly render the vehicle.".ConvertRichText();
			}
			if (properties.vehicleJobLimitations.NullOrEmpty())
			{
				yield return "<field>vehicleJobLimitations</field> list must be populated".ConvertRichText();
			}
			else
			{
				if (components.NullOrEmpty())
				{
					yield return "<field>components</field> must include at least 1 VehicleComponent".ConvertRichText();
				}
				if (!components.NullOrEmpty())
				{
					if (components.Select(c => c.key).GroupBy(s => s).Where(g => g.Count() > 1).Any())
					{
						yield return "<field>components</field> must not contain duplicate keys".ConvertRichText();
					}
					foreach (VehicleComponentProperties props in components)
					{
						foreach (string error in props.ConfigErrors())
						{
							yield return error;
						}
					}
				}
			}
		}

		public Vector2 ScaleDrawRatio(Vector2 size)
		{
			float sizeX = size.x;
			float sizeY = size.y;
			Vector2 drawSize = graphicData.drawSize;
			if (sizeX < sizeY)
			{
				sizeY = sizeX * (drawSize.y / drawSize.x);
			}
			else
			{
				sizeX = sizeY * (drawSize.x / drawSize.y);
			}
			return new Vector2(sizeX, sizeY);
		}

		public IEnumerable<VehicleStatCategoryDef> StatCategoryDefs()
		{
			if (speed > 0)
			{
				yield return VehicleStatCategoryDefOf.StatCategoryMovement;
			}
			yield return VehicleStatCategoryDefOf.StatCategoryArmor;

			foreach (VehicleCompProperties props in comps.Where(c => c is VehicleCompProperties))
			{
				foreach (VehicleStatCategoryDef statCategoryDef in props.StatCategoryDefs())
				{
					yield return statCategoryDef;
				}
			}
		}

		protected override void ResolveIcon()
		{
			if (graphic != null && graphic != BaseContent.BadGraphic)
			{
				Material material = graphic.ExtractInnerGraphicFor(null).MatAt(defaultPlacingRot, null);
				uiIcon = (Texture2D)material.mainTexture;
				uiIconColor = material.color;

				//PawnKindDef anyPawnKind = race.AnyPawnKind;
				//if (anyPawnKind != null)
				//{
				//	SWAP LIFESTAGES TO graphicData
				//	Material material2 = anyPawnKind.lifeStages.Last().bodyGraphicData.Graphic.MatAt(Rot4.East, null);
				//	uiIcon = (Texture2D)material2.mainTexture;
				//	uiIconColor = material2.color;
				//	return;
				//}
			}
		}

		/// <summary>
		/// Better performant CompProperties retrieval for VehicleDefs
		/// </summary>
		/// <remarks>Can only be called after all references have been resolved. If a CompProperties is needed beforehand, use <see cref="GetCompProperties{T}"/></remarks>
		/// <typeparam name="T"></typeparam>
		public T GetSortedCompProperties<T>() where T : CompProperties
		{
			for (int i = 0; i < cachedComps.Count; i++)
			{
				if (cachedComps[i] is T t)
				{
					cachedComps.CountIndex(i);
					return t;
				}
			}
			return default;
		}
	}
}
