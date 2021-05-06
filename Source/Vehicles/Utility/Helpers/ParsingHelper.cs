﻿using System;
using System.Reflection;
using System.Xml;
using System.Collections.Generic;
using Verse;
using SmashTools;
using SmashTools.Xml;

namespace Vehicles
{
	[LoadedEarly]
	public static class ParsingHelper
	{
		public static readonly Dictionary<string, HashSet<FieldInfo>> lockedFields = new Dictionary<string, HashSet<FieldInfo>>();

		internal static void RegisterParsers()
		{
			ParseHelper.Parsers<VehicleJobLimitations>.Register(new Func<string, VehicleJobLimitations>(VehicleJobLimitations.FromString));
			ParseHelper.Parsers<VehicleDamageMultipliers>.Register(new Func<string, VehicleDamageMultipliers>(VehicleDamageMultipliers.FromString));
			ParseHelper.Parsers<CompVehicleLauncher.DeploymentTimer>.Register(new Func<string, CompVehicleLauncher.DeploymentTimer>(CompVehicleLauncher.DeploymentTimer.FromString));
		}

		internal static void RegisterAttributes()
		{
			XmlParseHelper.RegisterAttribute("LockSetting", CheckFieldLocked);
			XmlParseHelper.RegisterAttribute("DisableSettings", CheckDisabledSettings);
		}

		private static void CheckFieldLocked(XmlNode node, string value, FieldInfo field)
		{
			if (value.ToUpperInvariant() == "TRUE")
			{
				XmlNode defNode = node.SelectSingleNode("defName");
				while (defNode is null)
				{
					XmlNode parentNode = node.ParentNode;
					if (parentNode is null)
					{
						Log.Error($"Cannot use LockSetting attribute on {field.Name} since it is not nested within a Def.");
						return;
					}
					defNode = parentNode.SelectSingleNode("defName");
				}
				string defName = defNode.InnerText;
				if (!field.HasAttribute<PostToSettingsAttribute>())
				{
					SmashLog.Error($"Cannont use LockSetting attribute on <field>{field.Name}</field> since related field does not have PostToSettings attribute in <type>{field.DeclaringType}</type>");
				}
				if (!lockedFields.ContainsKey(defName))
				{
					lockedFields.Add(defName, new HashSet<FieldInfo>());
				}
				lockedFields[defName].Add(field);
			}
		}

		private static void CheckDisabledSettings(XmlNode node, string value, FieldInfo field)
		{
			if (value.ToUpperInvariant() == "TRUE")
			{
				XmlNode defNode = node.SelectSingleNode("defName");
				if (defNode is null)
				{
					Log.Error($"Cannot use DisableSetting attribute on non-ThingDef XmlNodes.");
					return;
				}
				string defName = defNode.InnerText;
				VehicleMod.settingsDisabledFor.Add(defName);
			}
		}
	}
}