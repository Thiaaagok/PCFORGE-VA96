// =====================================================================
//  EA_secuencia_PN2.js   |   JScript de Enterprise Architect
//  ---------------------------------------------------------------------
//  Genera los 5 diagramas de SECUENCIA del PN2 (Compra de insumos):
//     CU08  Registrar orden de compra
//     CU09  Registrar o seleccionar proveedor
//     CU10  Generar solicitud de cotizacion
//     CU11  Aprobar solicitud de cotizacion
//     CU12  Recibir insumos y cerrar orden de compra
//
//  A diferencia de los que ya tenias, estos incorporan lo que faltaba y
//  que el PN1 si tenia (se usa el PN1 como plantilla):
//     * Digito verificador: <Entidad>BLL -> IntegridadBLL -> IntegridadMPP
//                           -> DAL.sp_DV_Recalcular()
//       (CalcularDV en altas, RecalcularDV en modificaciones)
//     * Bitacora:           <Entidad>BLL -> BitacoraBLL -> BitacoraMPP
//                           -> DAL.Registrar Evento()
//     * Chequeo de patente: <Entidad>BLL -> self ExigirPatente(...)
//
//  COMO USARLO
//    1. En el arbol del proyecto, seleccciona el paquete donde queres que
//       se creen (por ej. "N02.PN2" o un paquete nuevo de secuencia).
//    2. Start > Scripting > New JScript, pega este archivo y Run.
//    3. Revisa la pestania de salida "Script". Al terminar, abri cada
//       diagrama (quedan dentro del paquete "N02.PN2 Secuencia (DV+Bitacora)").
//
//  NOTAS (importante, leer)
//    * Cada lifeline se crea con su linea de vida estirada hasta debajo del
//      ultimo mensaje (DiagramObject.bottom) y los mensajes se ordenan con
//      SequenceNo, para que EA los apile y NO queden flotando.
//    * Si aun asi algun mensaje quedara pisado, movelo un poco: el contenido
//      (quien llama a quien) ya esta correcto.
//    * Los mensajes de retorno se crean en sentido inverso (devuelve ->
//      llama). Si EA los muestra con linea llena en vez de punteada,
//      click derecho sobre el mensaje > "Is Return Message" (o Advanced).
//      El script intenta marcarlos solo, pero esa propiedad varia entre
//      versiones de EA.
//    * No borra ni toca tus diagramas actuales: crea un paquete aparte.
//    * Suf. de clases: "96VA" (igual que en tus diagramas y la solucion).
//    * El UI (boundary) puede aparecer como RECTANGULO en vez de CIRCULO.
//      Es solo la notacion de EA: selecciona el/los "UI_96VA" (podes
//      seleccionar los 5 juntos con Ctrl), click derecho > Advanced >
//      "Use Circle Notation". Queda como el circulo de tus diagramas.
// =====================================================================

var SUFIJO = "96VA";
var NOMBRE_PAQUETE = "N02.PN2 Secuencia (DV+Bitacora)";

// anchos/altos de las cajas de lifeline (px)
var Y_TOP = 30;        // y del encabezado
var H_CAB = 40;        // alto del encabezado
var GAP_X = 55;        // separacion horizontal entre lifelines
var X0 = 25;           // x inicial

function log(m) { Session.Output(m); }

// ---------------------------------------------------------------------
//  Tipos de lifeline
//    "actor"    -> Actor (monigote)
//    "boundary" -> Boundary (el circulo de UI)
//    "life"     -> Sequence (caja de clase con linea de vida)
// ---------------------------------------------------------------------
function tipoEA(t) {
    if (t == "actor") return "Actor";
    if (t == "boundary") return "Boundary";
    return "Sequence";
}

function anchoCaja(nombre, tipo) {
    if (tipo == "actor") return 90;
    if (tipo == "boundary") return 90;
    var w = nombre.length * 7 + 24;
    if (w < 110) w = 110;
    return w;
}

// crea el elemento lifeline en el paquete y lo pone en el diagrama en x.
// 'prof' (negativo) es hasta donde baja la linea de vida: clave para que NO
// floten los mensajes. Primero se crea el encabezado y despues se estira el
// borde inferior (DiagramObject.bottom), igual que lo hace EA a mano.
function crearLifeline(pkg, dia, nombre, tipo, x, w, prof) {
    var e = pkg.Elements.AddNew(nombre, tipoEA(tipo));
    e.Update();
    pkg.Elements.Refresh();

    var top = (tipo == "boundary") ? 28 : 50;
    var geo = "l=" + x + ";r=" + (x + w) + ";t=" + top + ";b=-250;";
    var o = dia.DiagramObjects.AddNew(geo, "");
    o.ElementID = e.ElementID;
    o.Update();
    // estira la linea de vida hasta debajo del ultimo mensaje
    try { o.bottom = prof; o.Update(); } catch (e2) { }
    dia.DiagramObjects.Refresh();
    return e;
}

// crea un mensaje (connector Sequence). ret=true -> mensaje de retorno
function crearMensaje(mapa, from, to, texto, ret, orden) {
    var ef = mapa[from];
    var et = mapa[to];
    if (ef == null) { log("   !! lifeline origen no encontrado: " + from); return; }
    if (et == null) { log("   !! lifeline destino no encontrado: " + to); return; }

    var c = ef.Connectors.AddNew(texto, "Sequence");
    c.SupplierID = et.ElementID;
    c.Update();

    // orden vertical del mensaje (EA los apila por SequenceNo)
    try { c.SequenceNo = orden * 10; c.Update(); } catch (e1) { }
    if (ret) {
        try { c.Stereotype = "return"; c.Update(); } catch (e2) { }
        try { c.StyleEx = "LFSP=;Reply=1;"; c.Update(); } catch (e3) { }
    }
    ef.Connectors.Refresh();
}

// construye un diagrama completo a partir de su definicion
function construirDiagrama(pkg, def) {
    log("  > " + def.codigo + " - " + def.titulo);

    var dia = pkg.Diagrams.AddNew(def.codigo + " - DS " + def.titulo, "Sequence");
    dia.Update();
    pkg.Diagrams.Refresh();

    // profundidad de las lineas de vida: tiene que quedar DEBAJO del ultimo
    // mensaje (EA ubica cada mensaje ~35px mas abajo que el anterior).
    var prof = -(250 + def.mensajes.length * 34);

    // lifelines de izquierda a derecha
    var mapa = {};
    var x = X0;
    var i;
    for (i = 0; i < def.lifelines.length; i++) {
        var lf = def.lifelines[i];
        var w = anchoCaja(lf.n, lf.t);
        mapa[lf.n] = crearLifeline(pkg, dia, lf.n, lf.t, x, w, prof);
        x = x + w + GAP_X;
    }

    // mensajes en orden (SequenceNo define el orden vertical; EA los apila)
    for (i = 0; i < def.mensajes.length; i++) {
        var m = def.mensajes[i];
        crearMensaje(mapa, m[0], m[1], m[2], m.length > 3 ? m[3] : false, i + 1);
    }

    dia.Update();
    try { Repository.ReloadDiagram(dia.DiagramID); } catch (e) { }
    log("     lifelines: " + def.lifelines.length + "  mensajes: " + def.mensajes.length);
}

// ---------------------------------------------------------------------
//  Lifelines reutilizables
// ---------------------------------------------------------------------
function LF(n, t) { return { n: n, t: t }; }
var UI = LF("UI_" + SUFIJO, "boundary");
var INTE_BLL = LF("IntegridadBLL_" + SUFIJO, "life");
var INTE_MPP = LF("IntegridadMPP_" + SUFIJO, "life");
var BITA_BLL = LF("BitacoraBLL_" + SUFIJO, "life");
var BITA_MPP = LF("BitacoraMPP_" + SUFIJO, "life");
var DAL = LF("DAL_" + SUFIJO, "life");

// nombres (para no repetir strings)
var N_CI_BLL = "CompraInsumosBLL_" + SUFIJO;
var N_COMP_BLL = "ComponentesBLL_" + SUFIJO;
var N_COMP_MPP = "ComponentesMPP_" + SUFIJO;
var N_COMPRAS_MPP = "ComprasMPP_" + SUFIJO;
var N_PROV_BLL = "ProveedoresBLL_" + SUFIJO;
var N_PROV_MPP = "ProveedoresMPP_" + SUFIJO;
var N_INTE_BLL = INTE_BLL.n, N_INTE_MPP = INTE_MPP.n;
var N_BITA_BLL = BITA_BLL.n, N_BITA_MPP = BITA_MPP.n;
var N_DAL = DAL.n, N_UI = UI.n;

// bloque DV reutilizable (bll origen, metodo Calcular/Recalcular)
function bloqueDV(bll, metodo) {
    return [
        [bll, N_INTE_BLL, metodo + "()"],
        [N_INTE_BLL, N_INTE_MPP, metodo + "()"],
        [N_INTE_MPP, N_DAL, "sp_DV_Recalcular()"]
    ];
}
// bloque bitacora reutilizable
function bloqueBitacora(bll) {
    return [
        [bll, N_BITA_BLL, "Registrar Evento()"],
        [N_BITA_BLL, N_BITA_MPP, "Registrar Evento()"],
        [N_BITA_MPP, N_DAL, "Registrar Evento()"]
    ];
}
// concatena arrays de mensajes
function unir() {
    var r = [];
    var i, j;
    for (i = 0; i < arguments.length; i++)
        for (j = 0; j < arguments[i].length; j++)
            r.push(arguments[i][j]);
    return r;
}

// ---------------------------------------------------------------------
//  Definicion de los 5 diagramas
// ---------------------------------------------------------------------
function definiciones() {
    var defs = [];

    // ---- CU08 Registrar orden de compra ----
    defs.push({
        codigo: "CU08", titulo: "Registrar orden de compra",
        lifelines: [LF("Repositor", "actor"), UI, LF(N_CI_BLL, "life"), LF(N_COMP_BLL, "life"),
                    LF(N_COMPRAS_MPP, "life"), INTE_BLL, INTE_MPP, BITA_BLL, BITA_MPP, DAL],
        mensajes: unir([
            ["Repositor", N_UI, "seleccionar Nueva orden de compra()"],
            [N_UI, N_CI_BLL, "ObtenerFaltantes()"],
            [N_CI_BLL, N_COMP_BLL, "ObtenerBajoStock()"],
            [N_COMP_BLL, N_CI_BLL, ":List<Componente96VA>()", true],
            [N_CI_BLL, N_UI, "faltantes()", true],
            ["Repositor", N_UI, "elegirInsumos(cantidades)"],
            ["Repositor", N_UI, "ingresarFechaLimite(fecha)"],
            ["Repositor", N_UI, "Continuar()"],
            [N_UI, N_CI_BLL, "RegistrarOrdenCompra(faltantes, fechaLimite, repositor)"],
            [N_CI_BLL, N_CI_BLL, "ExigirPatente(RegistrarOrdenCompra)"],
            [N_CI_BLL, N_COMPRAS_MPP, "AgregarOrdenCompra(oc)"],
            [N_COMPRAS_MPP, N_DAL, "AgregarOrdenCompra(id, fechaLimite, dniRepositor)"],
            [N_DAL, N_COMPRAS_MPP, ":NumeroCompra()", true],
            [N_COMPRAS_MPP, N_DAL, "AgregarDetalle(idOrdenCompra, codigo, cantidad)"],
            [N_DAL, N_COMPRAS_MPP, ":OrdenCompra96VA (Pendiente)", true],
            [N_COMPRAS_MPP, N_CI_BLL, ":OrdenCompra96VA (Pendiente)", true]
        ],
            bloqueDV(N_CI_BLL, "CalcularDV"),
            bloqueBitacora(N_CI_BLL),
            [
                [N_CI_BLL, N_UI, ":OrdenCompra96VA (Pendiente)", true],
                [N_UI, "Repositor", "mostrar numero de compra()", true]
            ])
    });

    // ---- CU09 Registrar o seleccionar proveedor ----
    defs.push({
        codigo: "CU09", titulo: "Registrar o seleccionar proveedor",
        lifelines: [LF("Repositor", "actor"), UI, LF(N_PROV_BLL, "life"), LF(N_PROV_MPP, "life"),
                    INTE_BLL, INTE_MPP, BITA_BLL, BITA_MPP, DAL],
        mensajes: unir([
            ["Repositor", N_UI, "abrirSeleccionDeProveedor()"],
            [N_UI, N_PROV_BLL, "ObtenerTodos()"],
            [N_PROV_BLL, N_PROV_MPP, "ObtenerTodos()"],
            [N_PROV_MPP, N_DAL, "ObtenerTodos()"],
            [N_DAL, N_PROV_MPP, ":DataTable()", true],
            [N_PROV_BLL, N_UI, ":List<Proveedor96VA>()", true],
            ["Repositor", N_UI, "seleccionarProveedor(prov)"],
            ["Repositor", N_UI, "Nuevo proveedor()"],
            ["Repositor", N_UI, "cargarDatos(nombre, cuit, email, telefono, direccion)"],
            [N_UI, N_PROV_BLL, "Crear(proveedor)"],
            [N_PROV_BLL, N_PROV_BLL, "ExigirPatente(GestionarProveedores)"],
            [N_PROV_BLL, N_PROV_BLL, "ValidarCuitUnico(cuit)"],
            [N_PROV_BLL, N_PROV_MPP, "Agregar(p)"],
            [N_PROV_MPP, N_DAL, "Agregar(nombre, cuit, email, telefono, direccion)"],
            [N_DAL, N_PROV_MPP, "proveedor confirmado para la orden()", true],
            [N_PROV_MPP, N_PROV_BLL, "proveedor confirmado para la orden()", true]
        ],
            bloqueDV(N_PROV_BLL, "CalcularDV"),
            bloqueBitacora(N_PROV_BLL),
            [
                [N_PROV_BLL, N_UI, "proveedor confirmado para la orden()", true],
                [N_UI, "Repositor", "proveedor confirmado para la orden()", true]
            ])
    });

    // ---- CU10 Generar solicitud de cotizacion ----
    defs.push({
        codigo: "CU10", titulo: "Generar solicitud de cotizacion",
        lifelines: [LF("Repositor", "actor"), UI, LF(N_CI_BLL, "life"), LF(N_COMPRAS_MPP, "life"),
                    INTE_BLL, INTE_MPP, BITA_BLL, BITA_MPP, DAL],
        mensajes: unir([
            ["Repositor", N_UI, "abrirMesaDeCotizaciones()"],
            [N_UI, N_CI_BLL, "ObtenerOrdenesCompra()"],
            [N_UI, N_CI_BLL, "ObtenerCotizacionesPorOrden(idOrdenCompra)"],
            [N_CI_BLL, N_COMPRAS_MPP, "ObtenerCotizacionesPorOrden(id)"],
            [N_COMPRAS_MPP, N_DAL, "ObtenerCotizacionesPorOrden(id)"],
            [N_DAL, N_COMPRAS_MPP, ":DataTable()", true],
            [N_CI_BLL, N_UI, "ofertas de la orden()", true],
            ["Repositor", N_UI, "RegistrarOferta(proveedor, costo, condiciones)"],
            [N_UI, N_CI_BLL, "RegistrarCotizacion(idOrdenCompra, idProveedor, costo, condiciones)"],
            [N_CI_BLL, N_CI_BLL, "ExigirPatente(GestionarCotizaciones)"],
            [N_CI_BLL, N_COMPRAS_MPP, "AgregarCotizacion(cot)"],
            [N_COMPRAS_MPP, N_DAL, "AgregarCotizacion(numero, idOrdenCompra, idProveedor, costo, condiciones)"],
            [N_DAL, N_COMPRAS_MPP, ":PedidoCotizacion96VA (Por aprobar)", true],
            [N_COMPRAS_MPP, N_CI_BLL, ":PedidoCotizacion96VA (Por aprobar)", true]
        ],
            bloqueDV(N_CI_BLL, "CalcularDV"),
            bloqueBitacora(N_CI_BLL),
            [
                [N_CI_BLL, N_UI, ":PedidoCotizacion96VA (Por aprobar)", true],
                [N_UI, "Repositor", "mostrar la oferta en la mesa comparativa()", true]
            ])
    });

    // ---- CU11 Aprobar solicitud de cotizacion ----
    defs.push({
        codigo: "CU11", titulo: "Aprobar solicitud de cotizacion",
        lifelines: [LF("Gerente de compras", "actor"), UI, LF(N_CI_BLL, "life"), LF(N_COMPRAS_MPP, "life"),
                    INTE_BLL, INTE_MPP, BITA_BLL, BITA_MPP, DAL],
        mensajes: unir([
            ["Gerente de compras", N_UI, "abrirMesaDeCotizaciones()"],
            [N_UI, N_CI_BLL, "ObtenerCotizacionesPorOrden(idOrdenCompra)"],
            [N_CI_BLL, N_COMPRAS_MPP, "ObtenerCotizacionesPorOrden(id)"],
            [N_COMPRAS_MPP, N_DAL, "ObtenerCotizacionesPorOrden(id)"],
            [N_CI_BLL, N_UI, "ofertas Por aprobar()", true],
            ["Gerente de compras", N_UI, "revisarDetalle(oferta)"],
            ["Gerente de compras", N_UI, "Adjudicar()"],
            [N_UI, N_CI_BLL, "AprobarCotizacion(numeroCotizacion, gerente)"],
            [N_CI_BLL, N_CI_BLL, "ExigirPatente(AprobarCotizacion)"],
            [N_CI_BLL, N_COMPRAS_MPP, "CambiarEstadoCotizacion(numero, Aprobada, dniGerente)"],
            [N_COMPRAS_MPP, N_DAL, "CambiarEstadoCotizacion(numero, estado, dniGerente)"],
            [N_CI_BLL, N_COMPRAS_MPP, "CambiarEstadoCotizacion(las demas, Desaprobada)"],
            [N_CI_BLL, N_COMPRAS_MPP, "CambiarEstadoOrdenCompra(id, Enviada)"],
            [N_COMPRAS_MPP, N_DAL, "CambiarEstadoOrdenCompra(id, estado)"]
        ],
            bloqueDV(N_CI_BLL, "RecalcularDV"),
            bloqueBitacora(N_CI_BLL),
            [
                [N_CI_BLL, N_UI, "mostrar la oferta adjudicada()", true],
                [N_UI, "Gerente de compras", "mostrar la oferta adjudicada()", true]
            ])
    });

    // ---- CU12 Recibir insumos y cerrar orden de compra ----
    defs.push({
        codigo: "CU12", titulo: "Recibir insumos y cerrar orden de compra",
        lifelines: [LF("Repositor", "actor"), UI, LF(N_CI_BLL, "life"), LF(N_COMPRAS_MPP, "life"),
                    LF(N_COMP_MPP, "life"), INTE_BLL, INTE_MPP, BITA_BLL, BITA_MPP, DAL],
        mensajes: unir([
            ["Repositor", N_UI, "seleccionar Recepcion de insumos()"],
            ["Repositor", N_UI, "buscarOrden(numeroCompra)"],
            [N_UI, N_CI_BLL, "ObtenerPendienteDeRecibir(idOrdenCompra)"],
            [N_CI_BLL, N_COMPRAS_MPP, "ObtenerDetalle(id)"],
            [N_CI_BLL, N_COMPRAS_MPP, "ObtenerFacturasPorOrden(id)"],
            [N_CI_BLL, N_UI, ":List<DetalleComponente96VA>()", true],
            ["Repositor", N_UI, "marcarRecibidos(cantidades)"],
            ["Repositor", N_UI, "ConfirmarRecepcion()"],
            [N_UI, N_CI_BLL, "RegistrarFacturaCompra()"],
            [N_CI_BLL, N_CI_BLL, "ExigirPatente(RegistrarFacturaCompra)"],
            [N_CI_BLL, N_COMPRAS_MPP, "AgregarFacturaCompra(f)"],
            [N_COMPRAS_MPP, N_DAL, "AgregarFacturaCompra() + AgregarFacturaCompraDetalle()"],
            [N_CI_BLL, N_COMP_MPP, "SumarStock(codigo, cantidadRecibida)"],
            [N_COMP_MPP, N_DAL, "SumarStock(codigo, cantidad)"],
            [N_CI_BLL, N_COMPRAS_MPP, "CambiarEstadoOrdenCompra(id, Finalizada | RecibidaParcial)"],
            [N_COMPRAS_MPP, N_DAL, "CerrarOrdenCompra(id, fechaCierre)"]
        ],
            bloqueDV(N_CI_BLL, "RecalcularDV"),
            bloqueBitacora(N_CI_BLL),
            [
                [N_CI_BLL, N_UI, ":FacturaCompra96VA()", true],
                [N_UI, "Repositor", "mostrar estado y lo pendiente()", true]
            ])
    });

    return defs;
}

// ---------------------------------------------------------------------
//  main
// ---------------------------------------------------------------------
function buscarSubPaquete(padre, nombre) {
    var i;
    for (i = 0; i < padre.Packages.Count; i++) {
        var p = padre.Packages.GetAt(i);
        if (p.Name == nombre) return p;
    }
    return null;
}

function main() {
    Repository.EnsureOutputVisible("Script");
    log("=== EA_secuencia_PN2.js : generando diagramas de secuencia PN2 (con DV + Bitacora) ===");

    var sel = Repository.GetTreeSelectedPackage();
    if (sel == null) {
        Session.Prompt("Primero selecciona en el arbol el paquete donde crear los diagramas.", 0);
        return;
    }
    log("Paquete seleccionado: " + sel.Name);

    // crea (o reutiliza) el subpaquete contenedor
    var pkg = buscarSubPaquete(sel, NOMBRE_PAQUETE);
    if (pkg == null) {
        pkg = sel.Packages.AddNew(NOMBRE_PAQUETE, "Package");
        pkg.Update();
        sel.Packages.Refresh();
        log("Paquete creado: " + NOMBRE_PAQUETE);
    } else {
        log("Paquete ya existia, se agregan los diagramas dentro: " + NOMBRE_PAQUETE);
    }

    var defs = definiciones();
    var i;
    for (i = 0; i < defs.length; i++) {
        construirDiagrama(pkg, defs[i]);
    }

    try { Repository.RefreshModelView(sel.PackageID); } catch (e) { }
    log("=== Listo: " + defs.length + " diagramas de secuencia creados en '" + NOMBRE_PAQUETE + "'. ===");
    log("Recorda: si un mensaje queda pisado, movelo; si un retorno sale con linea llena, marcalo como 'Is Return'.");
    log("Si el UI sale como rectangulo: selecciona los 'UI_96VA' > click derecho > Advanced > 'Use Circle Notation'.");
}

main();
