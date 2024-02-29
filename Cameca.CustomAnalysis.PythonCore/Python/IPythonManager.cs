using System;

namespace Cameca.CustomAnalysis.PythonCore;

public interface IPythonManager : IDisposable
{
	bool Initialize();
	void Shutdown();
}
