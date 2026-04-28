using System.Collections;
using Features.Garage.Domain;
using Features.Garage.Infrastructure;
using Features.Unit.Infrastructure;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSetBUitkPreviewSceneDriver : MonoBehaviour
    {
        [SerializeField] private GarageSetBUitkRuntimeAdapter _adapter;
        [SerializeField] private ModuleCatalog _moduleCatalog;
        [SerializeField] private NovaPartVisualCatalog _novaPartVisualCatalog;
        [SerializeField] private NovaPartAlignmentCatalog _novaPartAlignmentCatalog;

        private GaragePageState _state;
        private GaragePagePresenter _presenter;
        private Coroutine _renderCoroutine;

        private void OnEnable()
        {
            if (!UnityEngine.Application.isPlaying)
                RenderPreviewLoadout();
        }

        private void Start()
        {
            if (UnityEngine.Application.isPlaying)
                _renderCoroutine = StartCoroutine(RenderWhenReady());
        }

        private void OnDisable()
        {
            if (_renderCoroutine != null)
            {
                StopCoroutine(_renderCoroutine);
                _renderCoroutine = null;
            }
        }

        private IEnumerator RenderWhenReady()
        {
            const int maxAttempts = 30;
            for (int i = 0; i < maxAttempts; i++)
            {
                if (RenderPreviewLoadout())
                    yield break;

                yield return null;
            }
        }

        private bool RenderPreviewLoadout()
        {
            if (_adapter == null || _moduleCatalog == null)
                return false;

            if (!_adapter.Bind())
                return false;

            var catalog = new GaragePanelCatalogFactory().Build(
                _moduleCatalog,
                _novaPartVisualCatalog,
                _novaPartAlignmentCatalog);
            if (!TryPickFrame(catalog, out var frame) ||
                !TryPickFirepower(catalog, out var firepower) ||
                !TryPickMobility(catalog, out var mobility))
                return false;

            _state ??= new GaragePageState();
            var roster = new GarageRoster();
            roster.SetSlot(0, new GarageRoster.UnitLoadout(
                frame.Id,
                firepower.Id,
                mobility.Id));
            _state.Initialize(roster);
            _presenter = new GaragePagePresenter(catalog);

            _adapter.Render(
                _presenter.BuildSlotViewModels(_state),
                _presenter.BuildEditorViewModel(_state),
                new GarageResultViewModel(
                    "UITK PREVIEW: 실제 Garage catalog 샘플",
                    "Preview scene driver가 실제 ModuleCatalog 첫 조합을 렌더링합니다.",
                    "Runtime smoke 전까지 저장 동작은 비활성입니다.",
                    isReady: false,
                    isDirty: false,
                    canSave: false,
                primaryActionLabel: "Preview Only"),
                GarageEditorFocus.Frame,
                isSaving: false);
            return true;
        }

        private static bool TryPickFrame(GaragePanelCatalog catalog, out GaragePanelCatalog.FrameOption frame)
        {
            frame = null;
            for (int i = 0; i < catalog.Frames.Count; i++)
            {
                if (catalog.Frames[i].PreviewPrefab == null)
                    continue;

                frame = catalog.Frames[i];
                return true;
            }

            return false;
        }

        private static bool TryPickFirepower(GaragePanelCatalog catalog, out GaragePanelCatalog.FirepowerOption firepower)
        {
            firepower = null;
            for (int i = 0; i < catalog.Firepower.Count; i++)
            {
                if (catalog.Firepower[i].PreviewPrefab == null)
                    continue;

                firepower = catalog.Firepower[i];
                return true;
            }

            return false;
        }

        private static bool TryPickMobility(GaragePanelCatalog catalog, out GaragePanelCatalog.MobilityOption mobility)
        {
            mobility = null;
            for (int i = 0; i < catalog.Mobility.Count; i++)
            {
                if (catalog.Mobility[i].PreviewPrefab == null)
                    continue;

                mobility = catalog.Mobility[i];
                return true;
            }

            return false;
        }
    }
}
