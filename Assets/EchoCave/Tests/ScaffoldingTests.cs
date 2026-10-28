using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class ScaffoldingTests
    {
        [Test]
        public void CoreAssembly_DoesNotReferenceUnityEngine()
        {
            Assembly core = Assembly.Load("EchoCave.Core");

            string[] unityReferences = core.GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor"))
                .ToArray();

            Assert.IsEmpty(unityReferences, "EchoCave.Core must stay engine-free.");
        }

        [Test]
        public void RokertCore_IsReachableFromTests()
        {
            var registry = new ServiceRegistry();

            Assert.IsNotNull(registry);
        }
    }
}
