using System.Collections;
using System.Collections.Generic;
using Luminix.Collections;

namespace Luminix;

public sealed partial class ModuleManager
{
    public readonly struct ModuleEnumerable : IEnumerable<Module>
    {
        private readonly ModuleManager manager;
        public ModuleEnumerable(ModuleManager manager) => this.manager = manager;

        public DenseMap<string, Module>.Enumerator GetEnumerator() => new(manager.modules);
        IEnumerator<Module> IEnumerable<Module>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public readonly struct InitializedModuleEnumerable : IEnumerable<Module>
    {
        private readonly ModuleManager manager;
        public InitializedModuleEnumerable(ModuleManager manager) => this.manager = manager;

        public DenseList<Module>.Enumerator GetEnumerator() => manager.initializedModules.GetEnumerator();
        IEnumerator<Module> IEnumerable<Module>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public readonly struct NonInitialModuleEnumerable : IEnumerable<Module>
    {
        private readonly ModuleManager manager;
        public NonInitialModuleEnumerable(ModuleManager manager) => this.manager = manager;

        public Enumerator GetEnumerator() => new(manager);
        IEnumerator<Module> IEnumerable<Module>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator: IEnumerator<Module>
        {
            public Enumerator(ModuleManager manager)
            {
                this.manager = manager;
                Reset();
            }

            private readonly ModuleManager manager;
            private Module current;

            private DenseMap<string, Module>.Enumerator innerEnumerator;

            public Module Current => current;
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                DenseList<string> initialModules = manager.initialModules;

                while (innerEnumerator.MoveNext())
                {
                    Module candidate = innerEnumerator.Current;

                    if (initialModules.Contains(candidate.Name))
                        continue;

                    current = candidate;
                    return true;
                }

                current = default;
                return false;
            }

            public void Reset()
            {
                innerEnumerator = manager.modules.RightItems.GetEnumerator();
                current = default;
            }

            public void Dispose() {
                innerEnumerator.Dispose();
                current = default;
            }
        }
    }

}