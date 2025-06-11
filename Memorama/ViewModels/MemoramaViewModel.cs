using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Memorama.Services;

namespace Memorama.ViewModels
{
    public class MemoramaViewModel:ObservableObject
    {
        MemoramaServer server = new();
        public ICommand IniciarCommand { get; set; }
        public ICommand DetenerCommand { get; set; }

        public MemoramaViewModel()
        {
            IniciarCommand = new RelayCommand(Iniciar);
            DetenerCommand = new RelayCommand(Detener);
        }

        void Iniciar()
        {
            try
            {
                server.Iniciar();
            }
            catch (HttpListenerException ex)
            {
                if (ex.Message.StartsWith("Acceso denegado"))
                {
                    //Necesito permisos para correr el servidor
                    ProcessStartInfo p = new ProcessStartInfo
                    {
                        FileName = "netsh.exe",
                        Arguments = "http add urlacl url=http://*:24000/memorama/ user=Todos",
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        Verb = "runas"
                    };

                    var res = Process.Start(p);
                    if (res != null)
                    {
                        res.WaitForExit();
                        if (res.ExitCode == 0)
                        {
                            server.Iniciar();
                        }
                    }
                }
            }



        }

        void Detener()
        {
            server.Detener();
        }
    }
}
