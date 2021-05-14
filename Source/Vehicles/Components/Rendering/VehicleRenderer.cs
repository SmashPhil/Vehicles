﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleRenderer
	{
		private const float SubInterval = 0.003787879f;
		private const float YOffset_Body = 0.007575758f;
		private const float YOffset_Wounds = 0.018939395f;
		private const float YOffset_Shell = 0.022727273f;
		private const float YOffset_Head = 0.026515152f;
		private const float YOffset_Status = 0.041666668f;

		private readonly VehiclePawn vehicle;

		public VehicleGraphicSet graphics;

		private readonly PawnHeadOverlays statusOverlays;

		private VehicleStatusEffecters effecters;

		private PawnWoundDrawer woundOverlays;

		private Graphic_Shadow shadowGraphic;

		public VehicleRenderer(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			statusOverlays = new PawnHeadOverlays(vehicle);
			woundOverlays = new PawnWoundDrawer(vehicle);
			graphics = new VehicleGraphicSet(vehicle);
			effecters = new VehicleStatusEffecters(vehicle);
		}

		public void RenderPawnAt(Vector3 drawLoc, float angle, bool northSouthRotation)
		{
			if (!graphics.AllResolved)
			{
				graphics.ResolveAllGraphics();
			}

			RenderPawnInternal(drawLoc, angle, northSouthRotation);

			if (vehicle.def.race.specialShadowData != null)
			{
				if (shadowGraphic == null)
				{
					shadowGraphic = new Graphic_Shadow(vehicle.def.race.specialShadowData);
				}
				shadowGraphic.Draw(drawLoc, Rot4.North, vehicle, 0f);
			}
			if (graphics.vehicle.VehicleGraphic != null && graphics.vehicle.VehicleGraphic.ShadowGraphic != null)
			{
				graphics.vehicle.VehicleGraphic.ShadowGraphic.Draw(drawLoc, Rot4.North, vehicle, 0f);
			}
			if (vehicle.Spawned && !vehicle.Dead)
			{
				//vehicle.stances.StanceTrackerDraw();
				vehicle.vPather.PatherDraw();
			}
		}

		public void RenderPortrait(bool northSouthRotation)
		{
			Vector3 zero = Vector3.zero;
			float angle;
			if (vehicle.Dead || vehicle.Downed)
			{
				angle = 85f;
				zero.x -= 0.18f;
				zero.z -= 0.18f;
			}
			else
			{
				angle = 0f;
			}
			RenderPawnInternal(zero, angle, Rot4.South, northSouthRotation, true);
		}

		private void RenderPawnInternal(Vector3 rootLoc, float angle, bool northSouthRotation)
		{
			vehicle.UpdateRotationAndAngle();
			RenderPawnInternal(rootLoc, angle, vehicle.Rotation, northSouthRotation, false);
		}

		private void RenderPawnInternal(Vector3 rootLoc, float angle, Rot4 bodyFacing, bool northSouthRotation, bool portrait)
		{
			if (!graphics.AllResolved)
			{
				graphics.ResolveAllGraphics();
			}
			Quaternion quaternion = Quaternion.AngleAxis(angle * (northSouthRotation ? -1 : 1), Vector3.up);

			Vector3 loc = rootLoc + vehicle.VehicleGraphic.DrawOffset(bodyFacing);
			loc.y += YOffset_Body;
			Rot8 vehicleRot = new Rot8(bodyFacing, angle);
			Mesh mesh = graphics.vehicle.VehicleGraphic.MeshAtFull(vehicleRot);
			List<Material> list = graphics.MatsBodyBaseAt(bodyFacing, RotDrawMode.Fresh);

			for (int i = 0; i < list.Count; i++)
			{
				GenDraw.DrawMeshNowOrLater(mesh, loc, quaternion, list[i], portrait);
				loc.y += SubInterval;
			}

			Vector3 drawLoc = rootLoc;
			drawLoc.y += YOffset_Wounds;
			woundOverlays.RenderOverBody(drawLoc, mesh, quaternion, portrait);

			Vector3 vector = rootLoc;
			Vector3 a = rootLoc;
			if (bodyFacing != Rot4.North)
			{
				a.y += YOffset_Head;
				vector.y += YOffset_Shell;
			}
			else
			{
				a.y += YOffset_Shell;
				vector.y += YOffset_Head;
			}
			//REDO
			if (!portrait && vehicle.RaceProps.Animal && vehicle.inventory != null && vehicle.inventory.innerContainer.Count > 0 && graphics.packGraphic != null)
			{
				Graphics.DrawMesh(mesh, vector, quaternion, graphics.packGraphic.MatAt(bodyFacing, null), 0);
			}
			if (!portrait)
			{
				Vector3 bodyLoc = rootLoc;
				bodyLoc.y += YOffset_Status;
				statusOverlays.RenderStatusOverlays(bodyLoc, quaternion, MeshPool.humanlikeHeadSet.MeshAt(bodyFacing));
			}
		}

		public void RendererTick()
		{
			effecters.EffectersTick();
		}
	}
}
