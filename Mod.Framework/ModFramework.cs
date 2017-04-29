﻿using Mono.Cecil;
using Ninject;
using Ninject.Extensions.Conventions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mod.Framework
{
	/// <summary>
	/// The bootstrapper for the Mod.Framework library.
	/// It will prepare Ninject and the registered modules
	/// </summary>
	public class ModFramework : IDisposable
	{
		private StandardKernel _kernel;
		private NugetAssemblyResolver _resolver;
		private ReaderParameters _readerParameters;
		private bool _initialised = false;

		public List<Assembly> Assemblies { get; set; } = new List<Assembly>();
		public List<AssemblyDefinition> CecilAssemblies { get; private set; } = new List<AssemblyDefinition>();

		public string DefaultModuleGlob { get; } = @"../../../Mod.Framework.**/bin/Debug/Mod.Framework.**.dll";

		public ModFramework(params Assembly[] module_assemblies)
		{
			this.Assemblies.Add(Assembly.GetExecutingAssembly());
			this.Assemblies.AddRange(module_assemblies);
		}

		#region Private methods
		private void Initialise()
		{
			if (!_initialised)
			{
				_resolver = new NugetAssemblyResolver();
				_readerParameters = new ReaderParameters(ReadingMode.Immediate)
				{
					AssemblyResolver = _resolver
				};

				_kernel = new StandardKernel();

				_kernel.Bind<ModFramework>().ToConstant(this);

				LoadExternalModules();

				_kernel.Bind(c => c.From(this.Assemblies)
					.SelectAllClasses()
					.WithAttribute<ModuleAttribute>()
					.BindBase()
				);

				_initialised = true;
			}
		}

		private void LoadExternalModules()
		{
			this.RegisterAssemblies(this.DefaultModuleGlob);
		}

		private void UpdateCecilAssemblies()
		{
			foreach (var assembly in this.Assemblies)
			{
				if (!CecilAssemblies.Any(x => x.FullName == assembly.FullName))
				{
					var def = AssemblyDefinition.ReadAssembly(assembly.Location, _readerParameters);
					CecilAssemblies.Add(def);
				}
			}
		}
		#endregion

		#region Public methods
		public void RegisterAssemblies(params Assembly[] assemblies)
		{
			foreach (var assembly in assemblies)
			{
				if (assembly == null || String.IsNullOrEmpty(assembly.Location))
					throw new Exception("Invalid Location for assembly");

				this.Assemblies.Add(assembly);
			}
		}

		public void RegisterAssemblies(params string[] globs)
		{
			foreach (var glob in globs)
			{
				foreach (var file in Glob.Glob.Expand(glob))
				{
					var assembly = Assembly.LoadFile(file.FullName);
					RegisterAssemblies(assembly);
				}
			}

			this.UpdateCecilAssemblies();
		}

		public void RunModules()
		{
			this.Initialise();

			foreach (RunnableModule module in _kernel.GetAll<RunnableModule>().OrderBy(x => x.Order))
			{
				module.Assemblies = module.AssemblyTargets.Count() == 0 ?
					this.CecilAssemblies
					: this.CecilAssemblies.Where(asm => module.AssemblyTargets.Any(t => t == asm.FullName))
				;

				Console.WriteLine($"\t-> Running module: {module.Name}");
				module.Run();
			}
		}
		#endregion

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					_kernel.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~Modder() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}