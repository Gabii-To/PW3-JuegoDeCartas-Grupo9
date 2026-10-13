namespace Truco.Net.Entidades

{
    internal enum Palo
    {
        ORO,
        COPA,
        ESPADA,
        BASTO
    }

    public class Carta
    {
        public int Numero { get; set; }
        public enum Palo { get; set; }

        public Carta(int numero, enum palo)
        {
            Numero = numero;
            Palo = palo;
        }

        public int ObtenerValorEscoba()
        {
            if (Numero >= 10)
            {
                return Numero - 2;
            }

            return Numero;
        }
    }
}