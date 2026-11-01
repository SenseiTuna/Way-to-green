using System;
using UnityEngine;

namespace KiyiDongusu
{
    // Field names intentionally match the single authoritative economy JSON.
    [Serializable] public sealed class MaterialDefinition
    {
        public string id, display_name_tr, facility_id;
        public int reserve_units, center_reserve_units, scattered_reserve_units;
        public int unlock_processed_total_units, base_batch_units, reward_cents_per_unit;
        public double full_base_batch_seconds;
    }
    [Serializable] public sealed class FacilityDefinition
    {
        public string id;
        public int[] shared_speed_costs_by_rank_credits, shared_capacity_costs_by_rank_credits;
    }
    [Serializable] public sealed class SourceDefinition
    {
        public int pile_units, scattered_units_per_click;
        public double pile_refill_seconds, base_click_work_units;
    }
    [Serializable] public sealed class CollectorDefinition
    {
        public int[] purchase_cost_credits;
        public double[] nominal_units_per_second_by_rank;
        public int scattered_priority_every_nth_completed_unit;
        public double fractional_work_carry_cap_if_blocked;
    }
    [Serializable] public sealed class ManualDefinition
    {
        public int[] purchase_cost_credits;
        public double[] work_per_click_by_rank;
    }
    [Serializable] public sealed class ProcessingDefinition
    {
        public int base_batch_capacity_per_lane, capacity_per_facility_rank;
        public double speed_multiplier_per_rank;
    }
    [Serializable] public sealed class AssistDefinition
    {
        public double minimum_accepted_input_interval_seconds;
        public double equivalent_processing_seconds_per_active_lane_per_click;
    }
    [Serializable] public sealed class FinalDefinition { public double automatic_processing_multiplier; }
    [Serializable] public sealed class TempoDefinition { public double multiplier; }
    [Serializable] public sealed class SortingNetDefinition { public int bonus_cents_per_unit; public bool reward_after_processing; }
    [Serializable] public sealed class SortingBinsDefinition { public int[] purchase_cost_credits; }
    [Serializable] public sealed class GameDefinitions
    {
        public string balance_id;
        public MaterialDefinition[] materials;
        public FacilityDefinition[] facilities;
        public SourceDefinition sources;
        public CollectorDefinition collector;
        public ManualDefinition manual_collection_upgrade;
        public ProcessingDefinition processing;
        public AssistDefinition assist;
        public FinalDefinition final_phase;
        public TempoDefinition tempo;
        public SortingNetDefinition sorting_net;
        public SortingBinsDefinition sorting_bins;

        public static GameDefinitions Load() => JsonUtility.FromJson<GameDefinitions>(Resources.Load<TextAsset>("Economy").text);
        public int FacilityIndex(int material) => Array.FindIndex(facilities, f => f.id == materials[material].facility_id);
    }
}
