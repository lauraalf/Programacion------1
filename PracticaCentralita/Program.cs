using System;
using System.Data.SqlClient;

namespace PracticaCentralita
{
    // 1. GESTOR DE BASE DE DATOS (CORREGIDO)
    public class GestorBaseDeDatos
    {
        // Conexión general para poder crear la base de datos desde cero
        private string conexionMaster = "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;";

        // Conexión directa a nuestra base de datos (una vez que ya está creada)
        private string conexionCentralita = "Server=(localdb)\\mssqllocaldb;Database=CentralitaDB;Trusted_Connection=True;";

        public void PrepararBaseDeDatos()
        {
            try
            {
                // PASO 1: Crear la Base de Datos (si no existe)
                using (SqlConnection conexion = new SqlConnection(conexionMaster))
                {
                    conexion.Open();
                    string scriptDB = @"
                        IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'CentralitaDB')
                        BEGIN
                            CREATE DATABASE CentralitaDB;
                        END;";

                    using (SqlCommand comando = new SqlCommand(scriptDB, conexion))
                    {
                        comando.ExecuteNonQuery();
                    }
                }

                // PASO 2: Crear la Tabla (conectándonos directamente a CentralitaDB)
                using (SqlConnection conexion = new SqlConnection(conexionCentralita))
                {
                    conexion.Open();
                    string scriptTabla = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RegistroLlamadas' and xtype='U')
                        BEGIN
                            CREATE TABLE RegistroLlamadas (
                                IdLlamada INT IDENTITY(1,1) PRIMARY KEY,
                                NumeroOrigen VARCHAR(20) NOT NULL,
                                NumeroDestino VARCHAR(20) NOT NULL,
                                Duracion DECIMAL(10, 2) NOT NULL,
                                CostoLlamada DECIMAL(10, 2) NOT NULL,
                                TipoLlamada VARCHAR(20) NULL,
                                FechaRegistro DATETIME DEFAULT GETDATE()
                            );
                        END;";

                    using (SqlCommand comando = new SqlCommand(scriptTabla, conexion))
                    {
                        comando.ExecuteNonQuery();
                        Console.WriteLine("[Éxito] Base de datos y tabla preparadas correctamente.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Error] al preparar BD: " + ex.Message + "");
            }
        }

        public void GuardarLlamadaEnBD(Llamada llamada, string tipo)
        {
            try
            {
                // Usamos la conexión que va directo a CentralitaDB
                using (SqlConnection conexion = new SqlConnection(conexionCentralita))
                {
                    conexion.Open();
                    string query = @"INSERT INTO RegistroLlamadas (NumeroOrigen, NumeroDestino, Duracion, CostoLlamada, TipoLlamada) 
                                     VALUES (@origen, @destino, @duracion, @costo, @tipo)";

                    using (SqlCommand comando = new SqlCommand(query, conexion))
                    {
                        comando.Parameters.AddWithValue("@origen", llamada.GetNumOrigen());
                        comando.Parameters.AddWithValue("@destino", llamada.GetNumDestino());
                        comando.Parameters.AddWithValue("@duracion", llamada.GetDuracion());
                        comando.Parameters.AddWithValue("@costo", llamada.CalcularPrecio());
                        comando.Parameters.AddWithValue("@tipo", tipo);

                        comando.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al guardar en BD: " + ex.Message);
            }
        }
    }

    // 2. CLASES DE LA PRÁCTICA (POO)
    public abstract class Llamada
    {
        protected string numOrigen;
        protected string numDestino;
        protected double duracion;

        public Llamada(string numOrigen, string numDestino, double duracion)
        {
            this.numOrigen = numOrigen;
            this.numDestino = numDestino;
            this.duracion = duracion;
        }

        public string GetNumOrigen() => numOrigen;
        public string GetNumDestino() => numDestino;
        public double GetDuracion() => duracion;

        public abstract double CalcularPrecio();
    }

    public class LlamadaLocal : Llamada
    {
        private double precio = 0.15;

        public LlamadaLocal(string numOrigen, string numDestino, double duracion)
            : base(numOrigen, numDestino, duracion) { }

        public override double CalcularPrecio() => duracion * precio;

        public override string ToString() => $"[Local] De: {numOrigen} a {numDestino} | {duracion}s | Total: {CalcularPrecio()} euros";
    }

    public class LlamadaProvincial : Llamada
    {
        private double precio1 = 0.20;
        private double precio2 = 0.25;
        private double precio3 = 0.30;
        private int franja;

        public LlamadaProvincial(string numOrigen, string numDestino, double duracion, int franja)
            : base(numOrigen, numDestino, duracion)
        {
            this.franja = franja;
        }

        public override double CalcularPrecio()
        {
            double precioActual = franja == 1 ? precio1 : (franja == 2 ? precio2 : (franja == 3 ? precio3 : 0));
            return duracion * precioActual;
        }

        public override string ToString() => $"[Provincial] De: {numOrigen} a {numDestino} | {duracion}s | Franja: {franja} | Total: {CalcularPrecio()} euros";
    }

    public class Centralita
    {
        private int cont = 0;
        private double acum = 0.0;
        private GestorBaseDeDatos bd = new GestorBaseDeDatos();

        public int GetTotalLlamadas() => cont;
        public double GetTotalFacturado() => acum;

        public void RegistrarLlamada(Llamada llamada)
        {
            cont++;
            acum += llamada.CalcularPrecio();

            // Imprimir en consola
            Console.WriteLine($"Registrada: {llamada.ToString()}");

            // Guardar en Base de Datos (determinamos el tipo para guardarlo)
            string tipo = llamada is LlamadaLocal ? "Local" : "Provincial";
            bd.GuardarLlamadaEnBD(llamada, tipo);
        }
    }

    // 3. CLASE PRINCIPAL (MAIN)
    class Program
    {
        static void Main(string[] args)
        {
            // 1. Crear base de datos automáticamente
            Console.WriteLine("Iniciando sistema...");
            GestorBaseDeDatos gestorBD = new GestorBaseDeDatos();
            gestorBD.PrepararBaseDeDatos();

            // 2. Lógica de la Centralita
            Centralita miCentralita = new Centralita();

            LlamadaLocal ll1 = new LlamadaLocal("8091112222", "8093334444", 45.0);
            LlamadaProvincial ll2 = new LlamadaProvincial("8095556666", "8297778888", 120.0, 1);
            LlamadaProvincial ll3 = new LlamadaProvincial("8099990000", "8491234567", 60.0, 3);

            Console.WriteLine("--- EMPEZANDO REGISTRO DE LA CENTRALITA ---");
            miCentralita.RegistrarLlamada(ll1);
            miCentralita.RegistrarLlamada(ll2);
            miCentralita.RegistrarLlamada(ll3);

            Console.WriteLine("--- REPORTE FINAL ---");
            Console.WriteLine($"Total de llamadas hechas: {miCentralita.GetTotalLlamadas()}");
            Console.WriteLine($"Total facturado (euros): {miCentralita.GetTotalFacturado():0.00}");

            Console.WriteLine("Presiona ENTER para salir...");
            Console.ReadLine();
        }
    }
}