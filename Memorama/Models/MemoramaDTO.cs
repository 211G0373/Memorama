using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memorama.Models
{
    public class MemoramaDTO
    {
        public int IdSesion { get; set; }
        public string Jugador1 { get; set; } = "";
        public string Jugador2 { get; set; } = "";
        public int PuntageJugador1 { get; set; } = 0;
        public int PuntageJugador2 { get; set; } = 0;
        public int Estado { get; set; }
        public string Turno { get; set; } = "";

        public int CartaRevelada { get; set; }
        public int PosCartaRevelada { get; set; }


    }
}
