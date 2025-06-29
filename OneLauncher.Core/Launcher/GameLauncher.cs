using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher;
public class GameLauncher
{
    public event Action? GameStartedEvent;
    public event Action? GameClosedEvent;
    public event Action<string>? GamePutEvent;
    public GameLauncher()
    { 
    
    }
}
