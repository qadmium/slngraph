using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace slngraph
{
    [DebuggerDisplay("Name = {Name}")]
    public class Project
    {
        public Project(IEnumerable<string> fragment)
        {
            var fragmentList = fragment.ToList();
            UseInGraphBuilding = true;
            Guid = new Guid(fragmentList.First().Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries).Last());
            Name = fragmentList.First().Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries).ElementAt(3);

            var depends = new List<Guid>();

            var enumerator = fragmentList.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Trim().StartsWith("ProjectSection(ProjectDependencies)"))
                {
                    while (enumerator.MoveNext() && !enumerator.Current.Trim().StartsWith("EndProjectSection"))
                    {
                        depends.Add(new Guid(enumerator.Current.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last()));
                    }
                }
            }

            DependsOnGuids = depends.Distinct();
        }

        public string Name { get; set; }

        public Guid Guid { get; set; }

        public bool UseInGraphBuilding { get; set; }
        
        public IEnumerable<Guid> DependsOnGuids { get; set; }

        public IEnumerable<Project> AllDependsOnProjects { get; set; }

        public IEnumerable<Project> ExplicitDependsOnProjects { get; set; }

        public bool IsDependsOn(Project project)
        {
            return AllDependsOnProjects.Contains(project) ||
                   AllDependsOnProjects.Any(prj => prj.IsDependsOn(project));
        }

        public IEnumerable<string> ToYuml(bool doNotShowImplicitDependency)
        {
            var dependencies = doNotShowImplicitDependency ? ExplicitDependsOnProjects.ToArray() : AllDependsOnProjects.ToArray();

            if (dependencies.Any())
            {
                return dependencies.Select(project => "[" + Name + "]->[" + project.Name + "]")
                    .Concat(
                            dependencies
                                .Select(project => project.ToYuml(doNotShowImplicitDependency))
                                .Aggregate((current, second) => current.Concat(second))
                        ).Distinct();    
            }

            return new[] { "[" + Name + "]" };
        }
    }
}
