﻿using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using Harmony;

namespace RimShips
{
    public enum ShipPermissions {NotAllowed, DriverNeeded, NoDriverNeeded}
    public enum ShipType {Paddles, Sails, Chemfuel, Nuclear}

    public class ShipProperties : CompProperties
    {
        public bool driverRequired = true;
        public bool drawDriver = false;
        public bool downable = false;
        public bool movesWhenDowned = false;

        public float cargoMax;
        public bool nameable;
        //public float rotationSpeed;
        public bool ejectPassengersWhenDestroyed = false;

        public string healthLabel_Healthy = "Operational";
        public string healthLabel_Injured = "Needs Repairs";
        public string healthLabel_Immobile = "Inoperable";
        public string healthLabel_Dead = "Sunken";
        public string healthLabel_Beached = "Beached";

        public ShipPermissions loadCargo = ShipPermissions.NotAllowed;
        public ShipPermissions moveable = ShipPermissions.DriverNeeded;
        public ShipPermissions armament = ShipPermissions.DriverNeeded;
        public ShipType shipPowerType = ShipType.Sails;

        public List<ShipRole> roles  = new List<ShipRole>();
        //public ListableOption<>

        public SoundDef soundWhileMoving = null;

        public ShipProperties()
        {
            this.compClass = typeof(CompShips);
        }

        public ShipProperties(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }


        
    }
}