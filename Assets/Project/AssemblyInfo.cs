using System.Runtime.CompilerServices;

// Expose Project assembly internals to the EditMode test assembly so white-box
// tests can validate internal types without widening their accessibility.
[assembly: InternalsVisibleTo("Project.Tests.EditMode")]
