namespace Truco.Net.Entidades
{
    internal class Mano
    {
        private List<Carta> cartas;

        public Mano()
        {
            cartas = new List<Carta>();
        }

        public List<Carta> Cartas => cartas;

        public int CantidadCartas()
        {
            return cartas.Count;
        }

        public void AgregarCarta(Carta carta)
        {
            cartas.Add(carta);
        }

        public void QuitarCarta(Carta carta)
        {
            cartas.Remove(carta);
        }

        public void Limpiar()
        {
            cartas.Clear();
        }
    }
}