﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.Jobs
{
    public class JobDriver_IdleShip : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override string GetReport()
        {
            return  ( !(Ship is null) ) ? "AwaitOrders".Translate() : base.GetReport();
        }

        private CompShips Ship
        {
            get
            {
                Thing thing = job.GetTarget(TargetIndex.A).Thing;
                if (thing is null) return null;
                return thing.TryGetComp<CompShips>();
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil wait = new Toil();
            wait.initAction = delegate ()
            {
                base.Map.pawnDestinationReservationManager.Reserve(this.pawn, this.job, this.pawn.Position);
                this.pawn.pather.StopDead();
            };
            wait.tickAction = delegate ()
            {
                if ((Find.TickManager.TicksGame + this.pawn.thingIDNumber) % JobSearchInterval == 0)
                {
                    this.CheckForNewJob();
                }
            };
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            yield return wait;
            yield break;
        }

        private void CheckForNewJob()
        {
            //Log.Message("Checking for caravan job");
        }

        private const int JobSearchInterval = 250;
    }
}