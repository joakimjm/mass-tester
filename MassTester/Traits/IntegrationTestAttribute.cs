using System;
using Xunit.Sdk;

namespace MassTester
{
    [TraitDiscoverer("MassTester.IntegrationTestDiscoverer", "MassTester")]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class IntegrationTestAttribute : Attribute, ITraitAttribute
    {
        public IntegrationTestAttribute() { }
    }
}