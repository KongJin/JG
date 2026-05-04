using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageSetBUitkDocumentHost
    {
        private const string ScreenName = "GarageSetBScreen";

        private readonly UIDocument _document;
        private readonly GarageSetBUitkRuntimeAdapter _adapter;

        public GarageSetBUitkDocumentHost(UIDocument document, GarageSetBUitkRuntimeAdapter adapter)
        {
            _document = document;
            _adapter = adapter;
        }

        public bool BindToHost(VisualElement host)
        {
            if (host == null || _adapter == null)
                return false;

            if (host.Q<VisualElement>(ScreenName) == null)
            {
                var source = _document?.visualTreeAsset;
                if (source == null)
                    return false;

                host.Clear();
                source.CloneTree(host);
            }

            return host.Q<VisualElement>(ScreenName) != null && _adapter.BindRoot(host);
        }

        public bool SetDocumentRootVisible(bool isVisible)
        {
            if (_document == null)
                return false;

            if (!_document.gameObject.activeSelf)
                _document.gameObject.SetActive(true);

            _document.sortingOrder = 10;
            var root = _document.rootVisualElement;
            if (root != null)
                root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

            return true;
        }
    }
}
