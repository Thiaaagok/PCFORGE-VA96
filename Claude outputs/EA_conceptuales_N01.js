!INC Local Scripts.EAConstants-JScript

/*
 * EA_conceptuales_N01.js  -  PCFORGE · N01.PN1 / N01.PN2  ·  DIAGRAMAS CONCEPTUALES (diagrama de clases del dominio)
 *
 * Script independiente para Enterprise Architect (Scripting > grupo "JScript").
 * Crea DENTRO DEL MODELO, para PN1 (Venta de computadoras) y PN2 (Compra de insumos):
 *   - las clases del dominio con sus atributos (los que nombra G02),
 *   - las asociaciones con nombre y multiplicidad,
 *   - un diagrama de clases "N01.PNx Diagrama conceptual" con todo ubicado.
 *
 * BALANCEO con los demas diagramas de N01 (secuencia, ECS, actividad):
 *   - Cada comportamiento Bk (mismos nombres/numeros que EA_generar_N01.js) declara que clases toca (C=crea, R=lee, U=actualiza).
 *   - Toda clase tiene que ser tocada por al menos un comportamiento (o ser el rol que ejecuta alguno).
 *   - Ningun comportamiento puede quedar sin clases, y ninguna clase puede quedar sin asociaciones.
 *   - Se imprime la matriz comportamiento x clase en la ventana System Output.
 *
 * COMO USARLO
 *   1. En el Project Browser hacer clic sobre el paquete donde se quieren crear los diagramas.
 *   2. Scripting > New Script (grupo JScript) > pegar TODO este archivo (incluida la primera linea !INC).
 *   3. Run (triangulo azul). Resultado y verificacion en la pestana "System Output".
 *   Si se vuelve a ejecutar, borra y recrea los paquetes "N01.PNx conceptual (balanceado)".
 *
 * Para cambiar algo (clases, atributos, relaciones, que comportamiento toca que clase) editar SOLO la funcion buildData().
 * Escrito en JScript clasico (sin let/const/flechas/indexOf de arrays) para el motor de EA.
 */

var BW = 210;      // ancho de cada clase
var GX = 70;       // separacion horizontal
var GY = 70;       // separacion vertical

// ---------------------------------------------------------------------------------------------
//  DATOS
//  clases : n = nombre | c,r = columna y fila en la grilla | rol = true si es un rol/persona | lane = rol del proceso que representa
//  asocs  : a, b = clases | label | ma = multiplicidad junto a "a" | mb = multiplicidad junto a "b"
//  comportamientos : n, name, lane (igual que en EA_generar_N01.js) | usa = "Clase:C|R|U,Clase:..."
// ---------------------------------------------------------------------------------------------
function buildData() {
    var P = [];

    P.push({
        id: "PN1", titulo: "Venta de computadoras",
        clases: [
            { n: "Recepcionista", c: 0, r: 0, rol: true, lane: "Recepcionista", attrs: ["nombre", "apellido", "dni"] },
            { n: "Cliente", c: 0, r: 1, rol: true, lane: "Cliente", attrs: ["nombre", "apellido", "dni", "teléfono", "dirección"] },
            { n: "Pago", c: 0, r: 2, attrs: ["monto", "fecha", "tipo (seña / saldo)"] },
            { n: "Recibo", c: 1, r: 0, attrs: ["montoAbonado", "saldoPendiente", "fechaEstimadaEntrega"] },
            { n: "Venta", c: 1, r: 1, attrs: ["fecha", "precioTotal"] },
            { n: "Factura", c: 1, r: 2, attrs: ["número", "fecha", "total"] },
            { n: "ModeloEstandar", c: 2, r: 0, attrs: ["nombre"] },
            { n: "Computadora", c: 2, r: 1, attrs: ["tipoConfiguración"] },
            { n: "OrdenDeProduccion", c: 2, r: 2, attrs: ["número", "fechaEntrega", "fechaInicio", "estado", "númeroDeSerie"] },
            { n: "Componente", c: 3, r: 1, attrs: ["código", "descripción", "stock"] },
            { n: "LineaEnsamblaje", c: 3, r: 2, attrs: ["nombre", "disponible"] },
            { n: "GerenteDeProduccion", c: 1, r: 3, rol: true, lane: "Gerente de producción", attrs: ["nombre", "apellido", "dni"] },
            { n: "ControlDeCalidad", c: 2, r: 3, attrs: ["fecha", "resultado", "observaciones"] },
            { n: "ResponsableTecnico", c: 3, r: 3, rol: true, lane: "Responsable técnico", attrs: ["nombre", "apellido", "dni"] }
        ],
        asocs: [
            { a: "Recepcionista", b: "Venta", label: "registra", ma: "1", mb: "0..*" },
            { a: "Cliente", b: "Venta", label: "solicita", ma: "1", mb: "0..*" },
            { a: "Venta", b: "Computadora", label: "incluye", ma: "1", mb: "1" },
            { a: "Venta", b: "Recibo", label: "emite", ma: "1", mb: "1" },
            { a: "Venta", b: "Pago", label: "se abona con", ma: "1", mb: "1..2" },
            { a: "Venta", b: "Factura", label: "se factura con", ma: "1", mb: "0..1" },
            { a: "Venta", b: "OrdenDeProduccion", label: "origina", ma: "1", mb: "0..1" },
            { a: "Computadora", b: "ModeloEstandar", label: "basada en", ma: "0..*", mb: "0..1" },
            { a: "Computadora", b: "Componente", label: "se arma con", ma: "0..*", mb: "1..*" },
            { a: "OrdenDeProduccion", b: "GerenteDeProduccion", label: "genera", ma: "0..*", mb: "1" },
            { a: "OrdenDeProduccion", b: "LineaEnsamblaje", label: "se ensambla en", ma: "0..*", mb: "0..1" },
            { a: "OrdenDeProduccion", b: "ResponsableTecnico", label: "a cargo de", ma: "0..*", mb: "0..1" },
            { a: "OrdenDeProduccion", b: "ControlDeCalidad", label: "se controla con", ma: "1", mb: "0..1" },
            { a: "ControlDeCalidad", b: "ResponsableTecnico", label: "registra", ma: "0..*", mb: "1" }
        ],
        comportamientos: [
            { n: 1, name: "Registrar computadora solicitada", lane: "Recepcionista", usa: "Venta:C,Computadora:C,Componente:R,ModeloEstandar:R" },
            { n: 2, name: "Buscar cliente por DNI", lane: "Recepcionista", usa: "Cliente:R" },
            { n: 3, name: "Registrar cliente nuevo", lane: "Recepcionista", usa: "Cliente:C" },
            { n: 4, name: "Registrar pago de seña y emitir recibo", lane: "Recepcionista", usa: "Pago:C,Recibo:C,Venta:R,Cliente:R,Computadora:R" },
            { n: 5, name: "Generar orden de producción", lane: "Gerente de producción", usa: "OrdenDeProduccion:C,Venta:R,Cliente:R,Computadora:R,Recibo:R" },
            { n: 6, name: "Asignar línea de ensamblaje", lane: "Gerente de producción", usa: "OrdenDeProduccion:U,LineaEnsamblaje:R" },
            { n: 7, name: "Registrar control de calidad", lane: "Responsable técnico", usa: "ControlDeCalidad:C,OrdenDeProduccion:R" },
            { n: 8, name: "Descontar stock de componentes", lane: "Responsable técnico", usa: "Componente:U,Computadora:R" },
            { n: 9, name: "Asignar N° de serie y finalizar orden", lane: "Responsable técnico", usa: "OrdenDeProduccion:U" },
            { n: 10, name: "Registrar pago del saldo", lane: "Recepcionista", usa: "Pago:C,Recibo:R,Venta:R" },
            { n: 11, name: "Emitir factura final", lane: "Recepcionista", usa: "Factura:C,Venta:R,Cliente:R,Pago:R" },
            { n: 12, name: "Cerrar orden de producción", lane: "Recepcionista", usa: "OrdenDeProduccion:U" }
        ]
    });

    P.push({
        id: "PN2", titulo: "Compra de insumos",
        clases: [
            { n: "Repositor", c: 1, r: 0, rol: true, lane: "Repositor", attrs: ["nombre", "apellido", "dni"] },
            { n: "GerenteDeCompras", c: 3, r: 0, rol: true, lane: "Gerente de compras", attrs: ["nombre", "apellido", "dni"] },
            { n: "DetalleDePedido", c: 0, r: 1, attrs: ["cantidad"] },
            { n: "PedidoDeCotizacion", c: 1, r: 1, attrs: ["número", "fechaEmisión", "fechaLímite", "estado", "usuarioAprobador"] },
            { n: "OrdenDeCompra", c: 3, r: 1, attrs: ["número", "fechaLímite", "estado", "fechaEntrega", "totalFacturado", "fechaCierre"] },
            { n: "DetalleDeOrden", c: 4, r: 1, attrs: ["cantidad"] },
            { n: "Proveedor", c: 2, r: 2, rol: true, lane: "Proveedor", attrs: ["nombre", "cuit", "email", "teléfono", "dirección"] },
            { n: "Componente", c: 2, r: 3, attrs: ["código", "descripción", "stock", "stockMínimo"] },
            { n: "Pago", c: 3, r: 3, attrs: ["montoAbonado", "fechaPago"] }
        ],
        asocs: [
            { a: "Repositor", b: "PedidoDeCotizacion", label: "genera", ma: "1", mb: "0..*" },
            { a: "Repositor", b: "OrdenDeCompra", label: "solicita", ma: "1", mb: "0..*" },
            { a: "GerenteDeCompras", b: "PedidoDeCotizacion", label: "aprueba", ma: "1", mb: "0..*" },
            { a: "GerenteDeCompras", b: "OrdenDeCompra", label: "registra", ma: "1", mb: "0..*" },
            { a: "Proveedor", b: "PedidoDeCotizacion", label: "cotiza", ma: "1", mb: "0..*" },
            { a: "Proveedor", b: "OrdenDeCompra", label: "provee", ma: "1", mb: "0..*" },
            { a: "PedidoDeCotizacion", b: "OrdenDeCompra", label: "origina", ma: "1", mb: "0..1" },
            { a: "PedidoDeCotizacion", b: "DetalleDePedido", label: "detalla", ma: "1", mb: "1..*" },
            { a: "DetalleDePedido", b: "Componente", label: "pide", ma: "0..*", mb: "1" },
            { a: "OrdenDeCompra", b: "DetalleDeOrden", label: "detalla", ma: "1", mb: "1..*" },
            { a: "DetalleDeOrden", b: "Componente", label: "recibe", ma: "0..*", mb: "1" },
            { a: "OrdenDeCompra", b: "Pago", label: "se paga con", ma: "1", mb: "0..1" }
        ],
        comportamientos: [
            { n: 1, name: "Detectar componentes con bajo stock", lane: "Repositor", usa: "Componente:R" },
            { n: 2, name: "Seleccionar proveedor", lane: "Repositor", usa: "Proveedor:R" },
            { n: 3, name: "Registrar proveedor nuevo", lane: "Repositor", usa: "Proveedor:C" },
            { n: 4, name: "Generar pedido de cotización", lane: "Repositor", usa: "PedidoDeCotizacion:C,DetalleDePedido:C,Componente:R,Proveedor:R" },
            { n: 5, name: "Verificar pedido de cotización", lane: "Gerente de compras", usa: "PedidoDeCotizacion:R,DetalleDePedido:R" },
            { n: 6, name: "Registrar aprobación / desaprobación del pedido", lane: "Gerente de compras", usa: "PedidoDeCotizacion:U" },
            { n: 7, name: "Registrar orden de compra y enviarla al proveedor", lane: "Gerente de compras", usa: "OrdenDeCompra:C,DetalleDeOrden:C,PedidoDeCotizacion:R,Proveedor:R" },
            { n: 8, name: "Verificar componentes recibidos", lane: "Repositor", usa: "OrdenDeCompra:U,DetalleDeOrden:R,Componente:R" },
            { n: 9, name: "Registrar pago al proveedor", lane: "Repositor", usa: "Pago:C,OrdenDeCompra:R" },
            { n: 10, name: "Actualizar stock y finalizar orden", lane: "Repositor", usa: "Componente:U,OrdenDeCompra:U" }
        ]
    });
    return P;
}

// ---------------------------------------------------------------------------------------------
//  Utilidades
// ---------------------------------------------------------------------------------------------
function log(s) { Session.Output(s); }
function rgb(r, g, b) { return r + g * 256 + b * 65536; }   // EA usa BGR
var COL_ROL = rgb(226, 235, 247);
var COL_ENT = rgb(255, 246, 199);

function has(arr, v) { var i; for (i = 0; i < arr.length; i++) { if (arr[i] === v) { return true; } } return false; }

function findClase(p, nombre) {
    var i;
    for (i = 0; i < p.clases.length; i++) { if (p.clases[i].n === nombre) { return p.clases[i]; } }
    return null;
}

function rect(x, y, w, h) { return "l=" + Math.round(x) + ";r=" + Math.round(x + w) + ";t=" + (-Math.round(y)) + ";b=" + (-Math.round(y + h)) + ";"; }

function deleteIfExists(parent, name) {
    var i;
    for (i = parent.Packages.Count - 1; i >= 0; i--) {
        if (parent.Packages.GetAt(i).Name === name) { parent.Packages.DeleteAt(i, false); }
    }
    parent.Packages.Refresh();
}

// "Clase:C,Clase:R" -> [{n:"Clase", op:"C"}, ...]
function parseUsa(texto) {
    var partes = texto.split(","), res = [], i, kv;
    for (i = 0; i < partes.length; i++) {
        kv = partes[i].split(":");
        res.push({ n: kv[0], op: kv[1] });
    }
    return res;
}

// ---------------------------------------------------------------------------------------------
//  Verificacion de balanceo (matriz comportamiento x clase)
// ---------------------------------------------------------------------------------------------
function verificar(p) {
    var ok = true, i, j, k, b, u, c, usos, tocada, conectada;
    function chk(cond, txt) { log("  " + (cond ? "OK  " : "ERR ") + txt); if (!cond) { ok = false; } }

    var tocadas = {};
    var nComp = p.comportamientos.length, sinClases = [], clasesInex = [], opInvalida = [];
    for (i = 0; i < nComp; i++) {
        b = p.comportamientos[i];
        usos = parseUsa(b.usa);
        if (usos.length === 0) { sinClases.push("B" + b.n); }
        for (j = 0; j < usos.length; j++) {
            if (findClase(p, usos[j].n) === null) { clasesInex.push("B" + b.n + "->" + usos[j].n); }
            if (usos[j].op !== "C" && usos[j].op !== "R" && usos[j].op !== "U") { opInvalida.push("B" + b.n + ":" + usos[j].n); }
            tocadas[usos[j].n] = true;
        }
        // el rol que ejecuta el comportamiento tambien cuenta como clase participante
        for (k = 0; k < p.clases.length; k++) { if (p.clases[k].lane === b.lane) { tocadas[p.clases[k].n] = true; } }
    }
    chk(sinClases.length === 0, sinClases.length ? "Comportamientos sin clases: " + sinClases.join(", ") : "Los " + nComp + " comportamientos tocan al menos una clase");
    chk(clasesInex.length === 0, clasesInex.length ? "Clases inexistentes: " + clasesInex.join(", ") : "Todas las clases usadas por los comportamientos existen");
    chk(opInvalida.length === 0, opInvalida.length ? "Operaciones invalidas (usar C, R o U): " + opInvalida.join(", ") : "Operaciones C/R/U validas");

    var huerfanas = [];
    for (i = 0; i < p.clases.length; i++) { if (!tocadas[p.clases[i].n]) { huerfanas.push(p.clases[i].n); } }
    chk(huerfanas.length === 0, huerfanas.length ? "Clases que ningun comportamiento usa: " + huerfanas.join(", ") : "Las " + p.clases.length + " clases son usadas por algun comportamiento");

    var asocMal = [], aislada = [];
    for (i = 0; i < p.asocs.length; i++) {
        if (findClase(p, p.asocs[i].a) === null || findClase(p, p.asocs[i].b) === null) { asocMal.push(p.asocs[i].a + "-" + p.asocs[i].b); }
    }
    chk(asocMal.length === 0, asocMal.length ? "Asociaciones con clases inexistentes: " + asocMal.join(", ") : "Las " + p.asocs.length + " asociaciones unen clases existentes");
    for (i = 0; i < p.clases.length; i++) {
        conectada = false;
        for (j = 0; j < p.asocs.length; j++) { if (p.asocs[j].a === p.clases[i].n || p.asocs[j].b === p.clases[i].n) { conectada = true; } }
        if (!conectada) { aislada.push(p.clases[i].n); }
    }
    chk(aislada.length === 0, aislada.length ? "Clases sin ninguna asociacion: " + aislada.join(", ") : "Ninguna clase queda aislada");

    var pos = {}, repetida = [];
    for (i = 0; i < p.clases.length; i++) {
        c = p.clases[i].c + "," + p.clases[i].r;
        if (pos[c]) { repetida.push(p.clases[i].n); }
        pos[c] = true;
    }
    chk(repetida.length === 0, repetida.length ? "Dos clases en la misma celda de la grilla: " + repetida.join(", ") : "Cada clase tiene su propia celda en el diagrama");

    // matriz
    log("");
    log("  Matriz comportamiento x clase (C=crea, R=lee, U=actualiza, *=rol que ejecuta)");
    var linea, op, celda, rolMarca;
    for (i = 0; i < nComp; i++) {
        b = p.comportamientos[i];
        usos = parseUsa(b.usa);
        linea = "  B" + b.n + " " + b.name + " [" + b.lane + "]: ";
        for (j = 0; j < usos.length; j++) { linea += usos[j].n + "(" + usos[j].op + ") "; }
        log(linea);
    }
    return ok;
}

// ---------------------------------------------------------------------------------------------
//  Creacion en EA
// ---------------------------------------------------------------------------------------------
function crearConceptual(pkg, p) {
    var diag = pkg.Diagrams.AddNew("N01." + p.id + " Diagrama conceptual", "Class");
    diag.Update();
    var i, j, c, el, at, els = {}, nRows = 0, nCols = 0;

    // alto de cada clase segun sus atributos
    for (i = 0; i < p.clases.length; i++) {
        c = p.clases[i];
        c.w = BW;
        c.h = 46 + c.attrs.length * 17;
        if (c.r + 1 > nRows) { nRows = c.r + 1; }
        if (c.c + 1 > nCols) { nCols = c.c + 1; }
    }
    var rowH = [], rowY = [], r, y = 40;
    for (r = 0; r < nRows; r++) {
        rowH[r] = 0;
        for (i = 0; i < p.clases.length; i++) { if (p.clases[i].r === r && p.clases[i].h > rowH[r]) { rowH[r] = p.clases[i].h; } }
        rowY[r] = y;
        y += rowH[r] + GY;
    }

    for (i = 0; i < p.clases.length; i++) {
        c = p.clases[i];
        el = pkg.Elements.AddNew(c.n, "Class");
        if (c.rol) { el.Stereotype = "rol"; }
        el.Update();
        for (j = 0; j < c.attrs.length; j++) {
            at = el.Attributes.AddNew(c.attrs[j], "string");
            at.Update();
        }
        el.Attributes.Refresh();
        els[c.n] = el;
        var x = 40 + c.c * (BW + GX);
        var yy = rowY[c.r] + (rowH[c.r] - c.h) / 2;
        var d = diag.DiagramObjects.AddNew(rect(x, yy, c.w, c.h), "");
        d.ElementID = el.ElementID;
        d.BackgroundColor = c.rol ? COL_ROL : COL_ENT;
        d.Update();
    }
    pkg.Elements.Refresh();

    for (i = 0; i < p.asocs.length; i++) {
        var a = p.asocs[i];
        var con = els[a.a].Connectors.AddNew(a.label, "Association");
        con.SupplierID = els[a.b].ElementID;
        con.Direction = "Unspecified";
        con.Update();
        con.ClientEnd.Cardinality = a.ma;
        con.ClientEnd.Update();
        con.SupplierEnd.Cardinality = a.mb;
        con.SupplierEnd.Update();
        con.Update();
        els[a.a].Connectors.Refresh();
    }
    Repository.ReloadDiagram(diag.DiagramID);
    return diag;
}

// ---------------------------------------------------------------------------------------------
//  main
// ---------------------------------------------------------------------------------------------
function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output("== N01 conceptuales balanceados: inicio ==");
    var parent = Repository.GetTreeSelectedPackage();
    if (parent === null || parent === undefined) {
        Session.Prompt("Seleccioná un paquete en el Project Browser y volvé a ejecutar el script.", promptOK);
        return;
    }
    var procesos = buildData(), i, allOk = true;
    for (i = 0; i < procesos.length; i++) {
        var p = procesos[i];
        log("");
        log("N01." + p.id + " " + p.titulo + "  (" + p.clases.length + " clases, " + p.asocs.length + " asociaciones, " + p.comportamientos.length + " comportamientos)");
        if (!verificar(p)) {
            allOk = false;
            log("  -> No se crea el diagrama de " + p.id + " hasta corregir los ERR.");
            continue;
        }
        var nombre = "N01." + p.id + " conceptual (balanceado)";
        deleteIfExists(parent, nombre);
        var pkg = parent.Packages.AddNew(nombre, "Package");
        pkg.Update();
        parent.Packages.Refresh();
        crearConceptual(pkg, p);
        log("  Diagrama creado: N01." + p.id + " Diagrama conceptual");
    }
    Repository.RefreshModelView(parent.PackageID);
    log("");
    log(allOk ? "== Listo: conceptuales balanceados ==" : "== Hay diagramas desbalanceados, revisar los ERR ==");
}

main();
