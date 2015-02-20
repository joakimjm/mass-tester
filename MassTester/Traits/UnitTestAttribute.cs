using System;
using Xunit.Sdk;

namespace MassTester
{
    [TraitDiscoverer("MassTester.UnitTestDiscoverer", "MassTester")]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class UnitTestAttribute : Attribute, ITraitAttribute
    {
        public UnitTestAttribute() { }
    }
}