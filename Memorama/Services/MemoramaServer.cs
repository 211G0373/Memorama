using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Memorama.Models;

namespace Memorama.Services
{
    public class MemoramaServer
    {
        HttpListener listener = new HttpListener();
        byte[] PaginaIndex;
        List<SesionJuego> sesiones = new();

        public MemoramaServer()
        {
            PaginaIndex = File.ReadAllBytes("assets/index.html");
        }

        public void Iniciar()
        {
            if (!listener.IsListening)
            {
                listener = new();
                listener.Prefixes.Add("http://*:24000/Memorama/");

                listener.Start();
                new Thread(EscucharPeticiones)
                { IsBackground = true }
                .Start();
            }
        }




        public void Detener()
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
        }


        void EscucharPeticiones()
        {
            try
            {
                var context = listener.GetContext();
                new Thread(EscucharPeticiones)
                { IsBackground = true }
                .Start();


                //Analizar las request

                if (context.Request.HttpMethod == "GET")
                {
                    switch (context.Request.RawUrl)
                    {
                        case "/memorama/":

                            PaginaIndex = File.ReadAllBytes("assets/index.html"); //Solo por las continuas modificaciones


                            context.Response.ContentLength64 = PaginaIndex.Length;
                            context.Response.ContentType = "text/html";
                            context.Response.StatusCode = 200;
                            context.Response.OutputStream.Write(PaginaIndex);
                            break;
                        default:

                            if (context.Request.RawUrl.StartsWith("/memorama/assets"))
                            {
                                string filePath = context.Request.RawUrl.Replace("/memorama/",""); // elimina el primer '/'

                                if (File.Exists(filePath))
                                {
                                    byte[] fileData = File.ReadAllBytes(filePath);

                                    // Detectar el tipo de contenido según la extensión
                                    string extension = Path.GetExtension(filePath).ToLower();
                                    string contentType = extension switch
                                    {
                                        ".jpg" or ".jpeg" => "image/jpeg",
                                        ".png" => "image/png",
                                        ".gif" => "image/gif",
                                        ".css" => "text/css",
                                        ".js" => "application/javascript",
                                        _ => "application/octet-stream"
                                    };

                                    context.Response.ContentLength64 = fileData.Length;
                                    context.Response.ContentType = contentType;
                                    context.Response.StatusCode = 200;
                                    context.Response.OutputStream.Write(fileData);
                                }
                                else
                                {
                                    context.Response.StatusCode = 404;
                                    byte[] notFound = Encoding.UTF8.GetBytes("Archivo no encontrado.");
                                    context.Response.OutputStream.Write(notFound);
                                }
                            }
                            else
                            {
                                // Si no es un archivo ni un caso especial, responder 404
                                context.Response.StatusCode = 404;
                                byte[] notFound = Encoding.UTF8.GetBytes("Página no encontrada.");
                                context.Response.OutputStream.Write(notFound);
                            }
                            break;

                    }
                }
                else if (context.Request.HttpMethod == "POST")
                {
                    switch (context.Request.RawUrl)
                    {
                        case "/memorama/quierojugar":
                            //1. Recuperar el nombre del jugador y su ip

                            var buffer = new byte[context.Request.ContentLength64];
                            context.Request.InputStream.Read(buffer);
                            var json = Encoding.UTF8.GetString(buffer);
                            var jugador = JsonSerializer.Deserialize<JugadorDTO>(json);

                            if (jugador != null)
                            {
                                //Tengo el nombre: jugador.Nombre
                                var ip = context.Request.RemoteEndPoint.Address.ToString();

                                //2. Verificar que el jugador no este ya jugando

                                SesionJuego? sesion = sesiones.FirstOrDefault(x =>
                                x.Jugador1 == jugador.Nombre && x.Ip1 == ip || x.Jugador2 == jugador.Nombre &&
                                x.Ip2 == ip);


                                if (sesion != null)//lo encontre en alguna sesión
                                {
                                    context.Response.StatusCode = 400; //BAD REQUEST
                                }
                                else
                                {
                                    //3. VERIFICAR SI HAY ALGUNA SESIÓN ESPERANDO JUGADOR
                                    sesion = sesiones.FirstOrDefault(x => x.EstaCompleto == false && x.Jugador1!=jugador.Nombre);

                                    if (sesion == null) //No hay sesiones disponibles, crear una
                                    {
                                        sesion = new SesionJuego()
                                        {
                                           
                                            Id = sesiones.OrderBy(x => x.Id).LastOrDefault()?.Id + 1 ?? 1, //Asignar un nuevo ID
                                        }; 
                                        sesion.AgregarJugador(jugador.Nombre, ip);
                                        sesiones.Add(sesion);
                                    }
                                    else //Soy el segundo jugador en esa sesión
                                    {
                                        sesion.AgregarJugador(jugador.Nombre, ip);
                                    }

                                    //Long polling para primer jugador
                                    while (sesion.EstaCompleto == false) //Conveniente agregarle un tiempo
                                    {
                                        Thread.Sleep(500);
                                    }


                                    ///revisar
                                    var gato = new MemoramaDTO() //Prepar dto para enviarlo a ambos
                                    {
                                        Estado = sesion.Estado,
                                        Jugador1 = sesion.Jugador1,
                                        Jugador2 = sesion.Jugador2,
                                        Turno = sesion.Turno,
                                        IdSesion = sesion.Id
                                    };

                                    byte[] dato = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(gato));

                                    context.Response.ContentLength64 = dato.Length;
                                    context.Response.ContentType = "application/json";
                                    context.Response.OutputStream.Write(dato);
                                    context.Response.StatusCode = 200; //ok



                                }

                            }





                            break;

                        case "/memorama/esperando": //el jugador que no tiene el turno espera que el otro juegue

                            //1. Recuperar el nombre del jugador y su ip

                            buffer = new byte[context.Request.ContentLength64];
                            context.Request.InputStream.Read(buffer);
                            json = Encoding.UTF8.GetString(buffer);
                            jugador = JsonSerializer.Deserialize<JugadorDTO>(json);

                            if (jugador != null)
                            {
                                var ip = context.Request.RemoteEndPoint.Address.ToString();


                                SesionJuego? sesion = sesiones.FirstOrDefault(x =>
                               x.Jugador1 == jugador.Nombre && x.Ip1 == ip || x.Jugador2 == jugador.Nombre &&
                               x.Ip2 == ip);

                                var lasturn = sesion?.LastTurno;
                                var turn = sesion?.Turno;

                                if (sesion == null)
                                {
                                    context.Response.StatusCode = 404; //No encontrado
                                }
                                else
                                {
                                    //long polling, mientras no sea mi turno


                                    for (int i = 0; i < 21; i++)
                                    {
                                        if (sesion.LastTurno == lasturn && sesion.Turno == turn)
                                        {
                                            
                                            Thread.Sleep(500);
                                            if (i == 20)
                                            {
                                                sesion.Estado = 4;
                                            }
                                            

                                        }
                                        else
                                        {
                                            break;
                                        }
                                        

                                    }





                                    var gato = new MemoramaDTO() //Prepar dto para enviarlo a ambos
                                    {
                                        Estado = sesion.Estado,
                                        Jugador1 = sesion.Jugador1,
                                        Jugador2 = sesion.Jugador2,

                                        PuntageJugador1 = sesion.PuntageJugador1,
                                        PuntageJugador2 = sesion.PuntageJugador2,
                                        CartaRevelada = sesion.UltimaCarta,
                                        PosCartaRevelada = sesion.PosUltimaCarta,
                                        Turno = sesion.Turno,
                                        IdSesion = sesion.Id
                                    };

                                    if (sesion.Estado == 3 || sesion.Estado == 4)
                                    {
                                        sesiones.Remove(sesion);
                                    }

                                    byte[] dato = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(gato));

                                    context.Response.ContentLength64 = dato.Length;
                                    context.Response.ContentType = "application/json";
                                    context.Response.OutputStream.Write(dato);
                                    context.Response.StatusCode = 200; //ok

                                    
                                }
                            }

                            break;


                        case "/memorama/jugada/":


                            //1. Recuperar el nombre del jugador y su ip

                            buffer = new byte[context.Request.ContentLength64];
                            context.Request.InputStream.Read(buffer);
                            json = Encoding.UTF8.GetString(buffer);
                            var jugada = JsonSerializer.Deserialize<JugadaDTO>(json);

                            if (jugada != null)
                            {

                                SesionJuego? sesion = sesiones.FirstOrDefault(x => x.Id == jugada.IdSesion);

                                if (sesion == null)
                                {
                                    context.Response.StatusCode = 404; //No encontrado
                                }
                                else
                                {

                                    if (sesion.ValidarTurno(jugada.Nombre) && sesion.ValidarMovimiento(jugada.PosCarta))
                                    {
                                        sesion.RevelarCarta(jugada.PosCarta);


                                        var gato = new MemoramaDTO() //Prepar dto para enviarlo a ambos
                                        {
                                            Estado = sesion.Estado,
                                            Jugador1 = sesion.Jugador1,
                                            Jugador2 = sesion.Jugador2,
                                            PuntageJugador1 = sesion.PuntageJugador1,
                                            PuntageJugador2 = sesion.PuntageJugador2,
                                            CartaRevelada = sesion.UltimaCarta,
                                            PosCartaRevelada = sesion.PosUltimaCarta,

                                            Turno = sesion.Turno,
                                            IdSesion = sesion.Id
                                        };

                                        byte[] dato = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(gato));

                                        context.Response.ContentLength64 = dato.Length;
                                        context.Response.ContentType = "application/json";
                                        context.Response.OutputStream.Write(dato);
                                        context.Response.StatusCode = 200; //ok

                                    }
                                    else
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                    }





                                }
                            }


                            break;
                    }
                }

                context.Response.Close();
            }
            catch
            {

            }
        }



    }
}
