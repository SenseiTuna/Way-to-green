using UnityEngine;
using UnityEngine.EventSystems;
namespace KiyiDongusu
{
    public sealed class UpgradeHover:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        CoastUpgradeTooltip tooltip;public PurchaseKind Kind{get;private set;}public int Facility{get;private set;}
        public void Initialize(CoastUpgradeTooltip hint,PurchaseKind kind,int facility){tooltip=hint;Kind=kind;Facility=facility;}
        public void OnPointerEnter(PointerEventData e){tooltip.Show(this);}public void OnPointerExit(PointerEventData e){tooltip.Hide(this);}
        public void OnSelect(BaseEventData e){tooltip.Show(this);}public void OnDeselect(BaseEventData e){tooltip.Hide(this);}
        void OnDisable(){if(tooltip!=null)tooltip.Hide(this);}
    }
}
