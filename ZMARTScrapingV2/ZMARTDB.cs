using LiteDB;
using System;
using System.Collections.Generic;

namespace ZMARTScrapingV2
{
    class ZMARTDB
    {
        private LiteDatabase db;
        public ZMARTDB()
        {
            db = new LiteDatabase(Environment.CurrentDirectory + "\\zmartdb.db");
        }

        public void IngresarInformacionJuego(Juego jg)
        {
            var col = db.GetCollection<Juego>("juegos");
            var resultado = col.Query().Where(x => x.nombre.Contains(jg.nombre)).FirstOrDefault();

            if(resultado == null)
            {
                col.Insert(jg);
            }
            else
            {
                if(jg.precio != resultado.precio)
                {
                    resultado.fecha_modificacion = DateTime.Today;
                    resultado.nuevo_precio = jg.precio;

                    if (jg.precio > resultado.precio)
                    {
                        resultado.observacion = "Subió de precio";
                    }
                    else
                    {
                        resultado.observacion = "Subió de precio";
                    }

                    col.Update(resultado);
                }
            }
        }
        public List<Juego> ObtenerInformacionJuegos()
        {
            var col = db.GetCollection<Juego>("juegos");
            var resultado = col.Query().ToList();

            return resultado;
        }
    }
}
