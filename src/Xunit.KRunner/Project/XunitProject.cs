using System;
using System.Collections.Generic;

namespace Xunit
{
    /// <summary>
    /// Represents an xUnit.net Test Project file (.xunit file)
    /// </summary>
    public class XunitProject
    {
        readonly List<XunitProjectAssembly> assemblies;

        /// <summary>
        /// Initializes a new instance of the <see cref="XunitProject"/> class.
        /// </summary>
        public XunitProject()
        {
            assemblies = new List<XunitProjectAssembly>();
        }

        /// <summary>
        /// Gets or sets the assemblies in the project.
        /// </summary>
        public IEnumerable<XunitProjectAssembly> Assemblies
        {
            get { return assemblies; }
        }

        /// <summary>
        /// Adds an assembly to the project
        /// </summary>
        /// <param name="assembly">The assembly to be added</param>
        public void AddAssembly(XunitProjectAssembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            assemblies.Add(assembly);
        }
    }
}
