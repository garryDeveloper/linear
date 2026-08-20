using MudBlazor;

namespace Linear.Web.Components.Common;

/// <summary>
/// Abre el diálogo de búsqueda, venga de donde venga el pedido.
/// </summary>
/// <remarks>
/// Existe porque hay dos formas de abrirlo —el botón de la cabecera y los atajos <c>/</c> y
/// <c>Ctrl+K</c>— y las dos tienen que compartir la misma guarda de "ya está abierto". Cuando
/// la guarda vivía en el componente del botón, el atajo tenía que entrar por ahí, lo que
/// obligaba a ese componente a registrar su propio listener; centralizarla acá es lo que
/// permite que el motor de atajos sea el único que escucha el teclado.
///
/// Es Scoped: el estado de "abierto" es por circuito, igual que el diálogo.
/// </remarks>
public sealed class SearchDialogLauncher(IDialogService dialogService)
{
    private bool _open;

    public async Task OpenAsync()
    {
        // Sin esta guarda, volver a apretar Ctrl+K con el buscador en pantalla apila un
        // segundo diálogo encima del primero.
        if (_open)
        {
            return;
        }

        _open = true;

        try
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            var dialog = await dialogService.ShowAsync<SearchDialog>(title: null, options);

            await dialog.Result;
        }
        finally
        {
            _open = false;
        }
    }
}
