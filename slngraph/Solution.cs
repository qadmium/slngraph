using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace slngraph
{
    public class Solution
    {
        public Solution(string path)
        {
            SolutionPath = path;
            Name = Path.GetFileNameWithoutExtension(path);

            var projects = new List<Project>();

            IEnumerable<string> lines = File.ReadAllLines(path);
            var enumerator = lines.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.StartsWith("Project"))
                {
                    var projectFragment = new List<string> {enumerator.Current};

                    while (enumerator.MoveNext() && !enumerator.Current.StartsWith("EndProject"))
                    {
                        projectFragment.Add(enumerator.Current);
                    }

                    projectFragment.Add(enumerator.Current);

                    projects.Add(new Project(projectFragment));
                }

                if (enumerator.Current.Trim().StartsWith("GlobalSection(ProjectDependencies)"))
                {
                    while (enumerator.MoveNext() && !enumerator.Current.Trim().StartsWith("EndGlobalSection"))
                    {
                        var splitted = enumerator.Current.Trim()
                                                    .Split(new[] {' ', '.'}, StringSplitOptions.RemoveEmptyEntries);

                        var projectGuid = new Guid(splitted.First().Trim());
                        var dependencyGuid = new Guid(splitted.Last().Trim());

                        var project = projects.Single(prj => prj.Guid == projectGuid);

                        project.DependsOnGuids = new List<Guid>(project.DependsOnGuids) { dependencyGuid };
                    }
                }
            }

            foreach (var project in projects)
            {
                var dependsList = project.DependsOnGuids.ToList();

                project.DependsOnGuids = dependsList.Distinct().ToList();

                project.AllDependsOnProjects = dependsList.Select(guid => projects.Single(proj => proj.Guid == guid)).ToList();
            }

            Projects = prepareExplicitDependecies(projects.OrderBy(project => project.Name));
        }


        public string SolutionPath { get; set; }

        public string Name { get; set; }

        public IEnumerable<Project> Projects { get; set; }

        public string ToYuml(bool doNotShowImplicitDependency)
        {
            var result = Projects
                .Where(project => project.UseInGraphBuilding)
                .Select(project => project.ToYuml(doNotShowImplicitDependency))
                .Aggregate((current, next) => current.Concat(next))
                .Distinct();

            return CleanUp(result).Aggregate((current, next) => current + ", " + next);
        }

        private IEnumerable<string> CleanUp(IEnumerable<string> set)
        {
            return set.Where(line => line.Contains("->") || IsStandaloneProject(line.Substring(1, line.Count() - 2)));
        }

        private bool IsStandaloneProject(string projectName)
        {
            var project = Projects.Single(prj => prj.Name == projectName);

            bool result = !Projects.Any(prj => prj.IsDependsOn(project));

            return result;
        }

        private IEnumerable<Project> prepareExplicitDependecies(IEnumerable<Project> projects)
        {
            var projectsList = projects.ToList();

            foreach (var project in projectsList)
            {
                var dependencies = project.AllDependsOnProjects.ToList();
                var result = new List<Project>();

                foreach (var dependency in dependencies)
                {
                    if (!dependencies.Any(dep => dep.IsDependsOn(dependency)))
                    {
                        result.Add(dependency);
                    }
                }

                project.ExplicitDependsOnProjects = result;
            }

            return projectsList;
        }
    }
}
