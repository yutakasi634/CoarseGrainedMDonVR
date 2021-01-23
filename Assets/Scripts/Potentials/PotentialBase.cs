using System.Collections.Generic;
using UnityEngine;

namespace Coral_iMD
{

    internal abstract class PotentialBase
    {
        internal abstract float  Potential (float r);
        internal abstract float  Derivative(float r);
        internal abstract string Name();
    }

} // Coral_iMD
