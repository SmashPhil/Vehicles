﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles.Lords;
using Vehicles.Defs;

namespace Vehicles
{
	internal class CaravanFormation : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.IsFormingCaravan)),
				prefix: new HarmonyMethod(typeof(CaravanFormation),
				nameof(IsFormingCaravanVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(TransferableUtility), nameof(TransferableUtility.CanStack)), prefix: null, postfix: null,
				transpiler: new HarmonyMethod(typeof(CaravanFormation),
				nameof(CanStackVehicleTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(GiveToPackAnimalUtility), nameof(GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace)),
				prefix: new HarmonyMethod(typeof(CaravanFormation),
				nameof(UsableVehicleWithMostFreeSpace)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), prefix: null, postfix: null,
				transpiler: new HarmonyMethod(typeof(CaravanFormation),
				nameof(AddHumanLikeOrdersLoadVehiclesTranspiler)));
		}

		/// <summary>
		/// Forming Caravan extension method based on Vehicle LordJob
		/// </summary>
		/// <param name="p"></param>
		/// <param name="__result"></param>
		public static bool IsFormingCaravanVehicle(Pawn p, ref bool __result)
		{
			Lord lord = p.GetLord();
			if (lord != null && (lord.LordJob is LordJob_FormAndSendVehicles))
			{
				__result = true;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Prevent stacking of Vehicles in the Dialog window of forming VehicleCaravan
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="ilg"></param>
		public static IEnumerable<CodeInstruction> CanStackVehicleTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];
				if (instruction.opcode == OpCodes.Stloc_0)
				{
					Label label = ilg.DefineLabel();

					i++;
					yield return instruction;
					yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
					yield return new CodeInstruction(opcode: OpCodes.Isinst, operand: typeof(VehiclePawn));
					yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

					yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(opcode: OpCodes.Ret);
					instruction = instructionList[i];
					instruction.labels.Add(label);
				}
				yield return instruction;
			}
		}

		/// <summary>
		/// Find Vehicle (Not pack animal) with usable free space for caravan packing
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="__result"></param>
		public static bool UsableVehicleWithMostFreeSpace(Pawn pawn, ref Pawn __result)
		{
			if(CaravanHelper.IsFormingCaravanShipHelper(pawn) || pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).HasVehicle())
			{
				__result = CaravanHelper.UsableVehicleWithTheMostFreeSpace(pawn);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Add FloatMenuOption(s) for Pawns towards Vehicles
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="ilg"></param>
		public static IEnumerable<CodeInstruction> AddHumanLikeOrdersLoadVehiclesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();
			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];
				if(instruction.LoadsField(AccessTools.Field(typeof(JobDefOf), nameof(JobDefOf.GiveToPackAnimal))))
				{
					yield return instruction; //Ldsfld : JobDefOf::GiveToPackAnimal
					instruction = instructionList[++i];
					Label jobLabel = ilg.DefineLabel();

					yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.makingFor)));
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Ext_Vehicles), nameof(Ext_Vehicles.HasVehicleInCaravan)));
					yield return new CodeInstruction(opcode: OpCodes.Brfalse, jobLabel);

					yield return new CodeInstruction(opcode: OpCodes.Pop);
					yield return new CodeInstruction(opcode: OpCodes.Ldsfld, AccessTools.Field(typeof(JobDefOf_Vehicles), nameof(JobDefOf_Vehicles.CarryItemToVehicle)));
					int j = i;
					while(j < instructionList.Count)
					{
						j++;
						if(instructionList[j].opcode == OpCodes.Stfld)
						{
							instructionList[j].labels.Add(jobLabel);
							break;
						}
					}
				}
				if(instruction.Calls(AccessTools.Property(typeof(Lord), nameof(Lord.LordJob)).GetGetMethod()))
				{
					yield return instruction;
					instruction = instructionList[++i];
					Label label = ilg.DefineLabel();
					Label label2 = ilg.DefineLabel();

					yield return new CodeInstruction(opcode: OpCodes.Dup);
					yield return new CodeInstruction(opcode: OpCodes.Isinst, operand: typeof(LordJob_FormAndSendVehicles));
					yield return new CodeInstruction(opcode: OpCodes.Brtrue, label);

					yield return instruction; //CASTCLASS : LordJob_FormAndSendCaravan
					instruction = instructionList[++i]; 
					yield return instruction; //STLOC_S : 50
					instruction = instructionList[++i];
					yield return instruction; //LDLOC_S : 49
					instruction = instructionList[++i];
					yield return instruction; //LDLOC_S : 50
					instruction = instructionList[++i];
					yield return instruction; //CALL : CapacityLeft
					instruction = instructionList[++i];
					yield return new CodeInstruction(opcode: OpCodes.Br, label2);

					yield return new CodeInstruction(opcode: OpCodes.Pop) { labels = new List<Label> { label } };
					yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 49);
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(LordUtility), "GetLord", new Type[] { typeof(Pawn) }));
					yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Property(typeof(Lord), nameof(Lord.LordJob)).GetGetMethod());
					yield return new CodeInstruction(opcode: OpCodes.Castclass, operand: typeof(LordJob_FormAndSendVehicles));
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(CaravanHelper), nameof(CaravanHelper.CapacityLeft)));

					instruction.labels.Add(label2);
					yield return instruction; //STFLD : capacityLeft
					instruction = instructionList[++i];
				}
				yield return instruction;
			}
		}
	}
}
