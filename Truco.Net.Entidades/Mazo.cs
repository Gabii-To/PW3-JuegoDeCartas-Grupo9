using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Truco.Net.Entidades
{
    public class Mazo
    {
        [Key]
        public int Id { get; set; }

        public List<Carta> Cartas { get; set; } = new List<Carta>();


        public void InicializarMazoEspanol()
        {
            Cartas.Clear();
            string[] palos = { "Oros", "Copas", "Espadas", "Bastos" };
            int[] numeros = { 1, 2, 3, 4, 5, 6, 7, 10, 11, 12 }; 

            foreach (var palo in palos)
            {
                foreach (var num in numeros)
                {
                    Cartas.Add(new Carta
                    {
                        Palo = palo,
                        Numero = num
                    });
                }
            }
        }

        public void MezclarCartas()
        {
            Random rng = new Random();
            Cartas = Cartas.OrderBy(_ => rng.Next()).ToList();
        }

        public List<Carta> RepartirCartas(int cantidad)
        {
            var cartasRepartidas = new List<Carta>();

            for (int i = 0; i < cantidad; i++)
            {
                if (Cartas.Count > 0)
                {
                    var carta = Cartas[0];
                    Cartas.RemoveAt(0); 
                    cartasRepartidas.Add(carta);
                }
            }

            return cartasRepartidas;
        }
    }
}
