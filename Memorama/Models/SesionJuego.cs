using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memorama.Models
{
    public class SesionJuego
    {
        public SesionJuego()
        {
            Random random = new Random();
            for(int i = 1; i < 9; i++)
            {
                Tablero.Add(i);
                Tablero.Add(i);

            }
            Tablero = Tablero.OrderBy(x => random.Next()).ToList();



        }
        public int PuntageJugador1 { get; set; } = 0;
        public int PuntageJugador2 { get; set; } = 0;
        public string Jugador1 { get; set; } = "";
        public string Jugador2 { get; set; } = "";
        public string Ip1 { get; set; } = "";
        public string Ip2 { get; set; } = "";
        public int Id { get; set; }
        public List<int> Tablero { get; set; } = new List<int>();
        public int PosUltimaCarta { get; set; } = -1;
        public int UltimaCarta { get; set; } = 0;

        public string Turno { get; set; } = "";
        public string LastTurno { get; set; } = "";

        public int Estado { get; set; }

        public bool EstaCompleto => Jugador1 != "" && Jugador2 != "";

        public void AgregarJugador(string nombre, string ip)
        {
            if (Jugador1 == "")
            {
                Jugador1 = nombre;
                Ip1 = ip;

                Estado = 0; //0 Esperando
                

                return; //Salirme para no revisar al segundo jugador
            }

            if (Jugador2 == "")
            {
                if (nombre == Jugador1)
                {
                    throw new ArgumentException("Los jugadores no puede tener el mismo nombre");
                }

                Jugador2 = nombre;
                Ip2 = ip;

                Estado = 1; //1 Jugando
            }

            Random random = new Random();
            Turno = random.Next(0, 2) == 0 ? Jugador1 : Jugador2; //Turno aleatorio
        }




        

        public bool ValidarTurno(string nombre)
        {
            return nombre == Turno;
        }

        public bool ValidarMovimiento(int n)
        {
            return !(Tablero[n] == 0  || (PosUltimaCarta == n && LastTurno==Turno));
        }

        public void RevelarCarta(int n)
        {



            if (Turno == LastTurno)
            {

                if (UltimaCarta == Tablero[n])
                {
                    //Acertó
                    if (Turno == Jugador1)
                    {
                        PuntageJugador1++;
                    }
                    else
                    {
                        PuntageJugador2++;
                    }
                    Estado = 2;//hay par
                    UltimaCarta = Tablero[n];
                    Tablero[n] = 0;
                    Tablero[PosUltimaCarta] = 0;
                    PosUltimaCarta = n;

                    LastTurno = Turno == Jugador1 ? Jugador2 : Jugador1;
                    
                }
                else
                {
                    UltimaCarta = Tablero[n];
                    PosUltimaCarta = n;
                    Estado = 1;


                    Turno = Turno == Jugador1 ? Jugador2 : Jugador1;

                }
            }
            else
            {
                UltimaCarta = Tablero[n];
                PosUltimaCarta = n;
                LastTurno = Turno;
                Estado = 1; //1 Jugando
            }






            





            if (VerificarCompleto())
            {




                Estado = 3; //Juego terminado
            }






        }

       

        public bool VerificarCompleto()
        {

            foreach (var item in Tablero)
            {
                if (item != 0)
                {
                    return false;
                }
            }

            return true;
        }


    }
}
