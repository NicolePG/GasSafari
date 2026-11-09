using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Una pantalla de los menús (docs/07 §2). Se construye una vez por código y se muestra o esconde desde MenuApp.
    // El botón Cancelar (Esc o B/Círculo) vuelve a la pantalla anterior.
    public abstract class MenuScreen : MonoBehaviour, ICancelHandler
    {
        public MenuApp App { get; private set; }
        public RectTransform Root => (RectTransform)transform;

        // Fondo oscuro translúcido detrás del contenido (se ve el escenario 3D del título).
        public virtual bool DimBackground => true;

        // Elemento que recibe el foco al abrir la pantalla.
        public virtual Selectable FirstSelected => null;

        // La pantalla maneja la entrada de cada jugador por su cuenta (lobby): se apaga la navegación global.
        public virtual bool HandlesOwnNavigation => false;

        internal void Init(MenuApp app)
        {
            App = app;
            UiFactory.Stretch(Root);
            Build();
        }

        protected abstract void Build();

        // Cada vez que la pantalla se muestra: refrescar saldos, candados y estados.
        public virtual void OnShow() { }

        public virtual void OnHide() { }

        // Se destruyó la raíz de los menús con esta pantalla abierta: soltar lo que escuchaba (eventos estáticos).
        protected virtual void Teardown() { }

        private void OnDestroy() => Teardown();

        // true si la pantalla gestionó el Cancelar.
        public virtual bool HandleCancel()
        {
            App.Back();
            return true;
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (HandleCancel())
                eventData.Use();
        }
    }
}
