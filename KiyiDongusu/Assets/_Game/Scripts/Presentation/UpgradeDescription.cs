using System.Text;
namespace KiyiDongusu
{
    public static class UpgradeDescription
    {
        static string T(string en,string tr)=>GameText.T(en,tr);
        static string Number(double value)=>value.ToString("0.##",GameText.Culture);
        public static string Title(PurchaseKind kind,int facility)=>kind==PurchaseKind.Collector?T("Collection crew","Toplama ekibi"):kind==PurchaseKind.Manual?T("Stronger hands","Toplama gücü"):GameText.Facility(facility)+" / "+(kind==PurchaseKind.Speed?T("Speed","Hız"):T("Batch capacity","Parti kapasitesi"));
        public static string Body(GameSession game,PurchaseKind kind,int facility)
        {
            int rank=game.Rank(kind,facility);long cost=game.Cost(kind,facility);bool max=cost<0;var d=game.Definitions;var text=new StringBuilder();
            if(kind==PurchaseKind.Collector||kind==PurchaseKind.Manual)
            {
                var values=kind==PurchaseKind.Collector?d.collector.nominal_units_per_second_by_rank:d.manual_collection_upgrade.work_per_click_by_rank;
                string units=kind==PurchaseKind.Collector?T(" pieces/s"," parça/sn"):T(" pieces/click"," parça/tık");
                text.AppendLine(kind==PurchaseKind.Collector?T("Collects pile and loose litter automatically, even without clicks.","Yığın ve tekil atıkları tıklama olmadan otomatik toplar."):T("Collects more from the main pile with each click.","Merkez yığından her tıklamada daha çok toplar."));
                text.AppendLine(T("Current: ","Mevcut: ")+Number(values[rank]*d.tempo.multiplier)+units);
                if(!max)text.AppendLine(T("Next: ","Sonraki: ")+Number(values[rank+1]*d.tempo.multiplier)+units);
                text.AppendLine(T("Collection gives raw stock. Credits arrive after recycling.","Toplama ham stok verir. Kredi işleme bitince gelir."));
            }
            else
            {
                text.AppendLine(kind==PurchaseKind.Speed?T("Processes all lines here faster. Shared lines keep running in parallel.","Buradaki bütün hatları hızlandırır. Ortak hatlar paralel çalışır."):T("Raises each line's batch limit and throughput. Existing batches keep their progress.","Her hattın parti sınırını ve işlem hızını artırır. Aktif partilerin ilerlemesi korunur."));
                double before=kind==PurchaseKind.Speed?1+d.processing.speed_multiplier_per_rank*rank:d.processing.base_batch_capacity_per_lane+d.processing.capacity_per_facility_rank*rank;
                double after=kind==PurchaseKind.Speed?before+d.processing.speed_multiplier_per_rank:before+d.processing.capacity_per_facility_rank;
                string unit=kind==PurchaseKind.Speed?"×":T(" pieces/line"," parça/hat");
                text.AppendLine(T("Current: ","Mevcut: ")+Number(before)+unit);if(!max)text.AppendLine(T("Next: ","Sonraki: ")+Number(after)+unit);
                for(int i=0;i<game.State.materials.Length;i++)if(d.FacilityIndex(i)==facility&&game.State.materials[i].unlocked)
                    text.AppendLine(GameText.Material(i)+": "+Number(game.Processing.Rate(i))+T("/s now","/sn şimdi")+(max?"":T(", ",", ")+Number(game.Processing.Rate(i)*after/before)+T("/s next","/sn sonra")));
            }
            bool locked=(kind==PurchaseKind.Speed||kind==PurchaseKind.Capacity)&&!game.FacilityUnlocked(facility);
            if(locked)
            {
                int threshold=int.MaxValue;for(int i=0;i<d.materials.Length;i++)if(d.FacilityIndex(i)==facility)threshold=System.Math.Min(threshold,d.materials[i].unlock_processed_total_units);
                text.Append(T("Free facility unlock: recycle ","Tesis ücretsiz açılır: ")+threshold+T(" pieces. "," parça işle. ")+game.Processed+" / "+threshold);
            }
            else if(max)text.Append(T("Maximum level reached.","En yüksek seviyede."));
            else
            {
                text.Append(T("Cost: ","Fiyat: ")+Number(cost/100.0)+T(" credits"," kredi"));
                if(game.State.money<cost)text.AppendLine().Append(T("Need ","Gereken ek: ")+Number((cost-game.State.money)/100.0)+T(" more credits."," kredi."));
            }
            return text.ToString();
        }
    }
}
