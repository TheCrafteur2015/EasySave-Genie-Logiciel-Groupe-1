/// <summary>
/// Configures the parallel execution policy for the test assembly.
/// </summary>
/// <remarks>
/// This attribute enables parallel execution of tests at the method level. 
/// It allows distinct test methods to run concurrently, thereby reducing the overall time required to execute the test suite.
/// </remarks>
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]