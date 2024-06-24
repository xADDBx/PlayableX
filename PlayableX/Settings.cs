using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace PlayableX {
    public class Settings : UnityModManager.ModSettings {
        public bool enableMoreThanOneNavigatorInParty = true;
        public override void Save(UnityModManager.ModEntry modEntry) {
            Save(this, modEntry);
        }
    }
}