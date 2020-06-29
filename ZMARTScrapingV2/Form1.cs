using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Windows.Forms;

namespace ZMARTScrapingV2
{
    public partial class frmPrincipal : Form
    {
        private DataTable _tablaDatos = null;
        private DataSet _conjuntoDatos = null;

        private SortedDictionary<int, string> _filtroGuardado = new SortedDictionary<int, string>();
        private SortedDictionary<int, string> _ordenGuardado = new SortedDictionary<int, string>();

        private ZMARTDB bd = new ZMARTDB();

        private delegate void AgregarATablaDelegate(object[] fila);
        private delegate void ActivarBotonExtraccionDelegate();
        public frmPrincipal()
        {
            InitializeComponent();

            _filtroGuardado.Add(0, "");
            _ordenGuardado.Add(0, "");

            _tablaDatos = new DataTable();
            _conjuntoDatos = new DataSet();

            vinculoFuente.DataSource = _conjuntoDatos;
            
            advancedDataGridView1.SetDoubleBuffered();
            advancedDataGridView1.DataSource = vinculoFuente;

            EstablecerPruebaDatos();
        }

        private void EstablecerPruebaDatos()
        {
            _tablaDatos = _conjuntoDatos.Tables.Add("tablaPrueba");
            _tablaDatos.Columns.Add("_id", typeof(int));
            _tablaDatos.Columns.Add("nombre", typeof(string));
            _tablaDatos.Columns.Add("precio", typeof(int));
            _tablaDatos.Columns.Add("fecha_descarga", typeof(DateTime));
            _tablaDatos.Columns.Add("nuevo_precio", typeof(int));
            _tablaDatos.Columns.Add("fecha_modificacion", typeof(DateTime));
            _tablaDatos.Columns.Add("observacion", typeof(string));

            vinculoFuente.DataMember = _tablaDatos.TableName;

            advancedDataGridViewSearchToolBar1.SetColumns(advancedDataGridView1.Columns);
        }

        private void btnExtraer_Click(object sender, EventArgs e)
        {
            _tablaDatos.Rows.Clear();

            Thread hilo = new Thread(new ThreadStart(Extraccion));
            hilo.Start();

            btnExtraer.Enabled = false;
        }

        private void Extraccion()
        {
            txtEstado.Text = "Cargando...";

            ChromeDriver driver = new ChromeDriver();
            driver.Url = "https://www.zmart.cl/JuegosPS4";

            Thread.Sleep(400);

            while (driver.FindElementByClassName("ProdDisplayType5_MasProductos").Displayed)
            {
                driver.FindElementByClassName("ProdDisplayType5_MasProductos").Click();
            }

            txtEstado.Text = "Comparando información con base de datos...";

            foreach (var oJuego in driver.FindElements(By.CssSelector("div[class='BoxProductoS2 BorderPlatPS4'")))
            {

                string titulo = oJuego.FindElement(By.CssSelector("div[class='BoxProductoS2_Descripcion']")).Text;
                int precio = 0;
                DateTime fecha = DateTime.Today;

                if (Int32.TryParse(oJuego.FindElement(By.CssSelector("span[class='BoxProductoS2_Precio']")).Text.Replace(".", ""), out int pr))
                {
                    precio = pr;
                }

                var nJuego = new Juego
                {
                    nombre = titulo,
                    precio = precio,
                    fecha_descarga = fecha,
                    nuevo_precio = 0,
                    fecha_modificacion = DateTime.Today,
                    observacion = ""
                };

                bd.IngresarInformacionJuego(nJuego);
            }

            driver.Close();
            driver.Quit();

            foreach (var reg in bd.ObtenerInformacionJuegos())
            {
                object[] registro = new object[]
                {
                    reg.Id,
                    reg.nombre,
                    reg.precio,
                    reg.fecha_descarga,
                    reg.nuevo_precio,
                    reg.fecha_modificacion,
                    reg.observacion
                };

                //_tablaDatos.Rows.Add(registro);
                var dt = new AgregarATablaDelegate(AgregarATabla);
                advancedDataGridView1.Invoke(dt, new object[] { registro });
                
            }

            txtEstado.Text = "Extracción completada";

            var at = new ActivarBotonExtraccionDelegate(ActivarBotonExtraccion);
            btnExtraer.Invoke(at, new object[] { });
        }

        private void AgregarATabla(object[] fila)
        {
            _tablaDatos.Rows.Add(fila);
        }

        private void ActivarBotonExtraccion()
        {
            btnExtraer.Enabled = true;
        }

        private void frmPrincipal_Load(object sender, EventArgs e)
        {
            txtEstado.Text = "Sin datos";

            advancedDataGridView1.DisableFilterAndSort(advancedDataGridView1.Columns["_id"]);
            advancedDataGridView1.SetFilterDateAndTimeEnabled(advancedDataGridView1.Columns["fecha_descarga"], true);
            advancedDataGridView1.SetFilterDateAndTimeEnabled(advancedDataGridView1.Columns["fecha_modificacion"], true);
        }

        private void advancedDataGridViewSearchToolBar1_Search(object sender, Zuby.ADGV.AdvancedDataGridViewSearchToolBarSearchEventArgs e)
        {
            bool resetearBusqueda = true;
            int inicioColumna = 0;
            int inicioFila = 0;

            if (!e.FromBegin)
            {
                bool finColumna = advancedDataGridView1.CurrentCell.ColumnIndex + 1 >= advancedDataGridView1.ColumnCount;
                bool finFila = advancedDataGridView1.CurrentCell.RowIndex + 1 >= advancedDataGridView1.RowCount;

                if(finColumna && finFila)
                {
                    inicioColumna = advancedDataGridView1.CurrentCell.ColumnIndex;
                    inicioFila = advancedDataGridView1.CurrentCell.RowIndex;
                }
                else
                {
                    inicioColumna = finColumna ? 0 : advancedDataGridView1.CurrentCell.ColumnIndex + 1;
                    inicioFila = advancedDataGridView1.CurrentCell.RowIndex + (finColumna ? 1 : 0);
                }

                DataGridViewCell c = advancedDataGridView1.FindCell(
                    e.ValueToSearch,
                    e.ColumnToSearch != null ? e.ColumnToSearch.Name : null,
                    inicioFila,
                    inicioColumna,
                    e.WholeWord,
                    e.CaseSensitive
                );

                if(c == null && resetearBusqueda)
                {
                    c = advancedDataGridView1.FindCell(
                        e.ValueToSearch,
                        e.ColumnToSearch != null ? e.ColumnToSearch.Name : null,
                        0,
                        0,
                        e.WholeWord,
                        e.CaseSensitive
                    );
                }

                if (c != null)
                    advancedDataGridView1.CurrentCell = c;
            }
        }
    }
}
