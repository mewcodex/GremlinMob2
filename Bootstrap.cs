using MegaCrit.Sts2.Core.Models.CardPools;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

[ModInitializer(nameof(Initialize))]
public static class Bootstrap
{
    private const string ModId = "mewcodex.gremlin-mob-2";
    private static bool _registered;

    internal static Type[] Cards { get; } = typeof(MobCard).Assembly.GetTypes()
        .Where(type => type.IsSubclassOf(typeof(MobCard)) && !type.IsAbstract)
        .OrderBy(type => type.Name, StringComparer.Ordinal).ToArray();

    private static readonly Type[] GeneratedCards =
    [
        typeof(MobPaperWard), typeof(MobBurst), typeof(MobRampageRoar), typeof(MobRampageBash), typeof(MobRampageRush),
        typeof(MobBruteOption), typeof(MobBruiserOption), typeof(MobGuardOption), typeof(MobScoutOption), typeof(MobMageOption)
    ];
    internal static IEnumerable<Type> CharacterCards => Cards.Except(GeneratedCards);

    private static readonly Type[] OtherModels =
    [
        typeof(GremlinMobCharacter), typeof(MobCards), typeof(MobRelics), typeof(MobPotions),
        typeof(MobRandomHandoffPotion), typeof(MobCrewTonic), typeof(MobReturningSpirit),
        typeof(MobRotationBell), typeof(MobMadStrength), typeof(MobWeakenedStrength), typeof(MobTemporaryThorns),
        typeof(MobReserveTalisman), typeof(MobReserveBomb), typeof(MobChargeMallet), typeof(MobSpellStaff),
        typeof(MobRotatingBell), typeof(MobRecruitPennant), typeof(MobSteadyMuscle), typeof(MobScattershotRelic),
        typeof(MobSpareGrave), typeof(MobSpellHat), typeof(MobWoundsRelic), typeof(MobPledgedMember),
        typeof(MobPilferedGoods)
    ];

    public static void Initialize()
    {
        new Harmony(ModId).PatchAll();
        foreach (var type in GeneratedCards)
            ModHelper.AddModelToPool(typeof(TokenCardPool), type);

        // The native hook list skips melted relics. Use one listener per phase to keep the squad active.
        ModHelper.SubscribeForRunStateHooks(ModId, run => MeltedRosters(run.Players
            .Where(player => player.Creature.CombatState?.IsLiveCombat() != true)));
        ModHelper.SubscribeForCombatStateHooks(ModId, combat => combat.IsLiveCombat() ? MeltedRosters(combat.Players) : []);
    }

    private static IEnumerable<AbstractModel> MeltedRosters(IEnumerable<Player> players) => players
        .Where(player => player.IsActiveForHooks)
        .Select(MobRosterRelic.For)
        .OfType<MobRosterRelic>()
        .Where(roster => roster.IsMelted);

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
    private static class Register
    {
        [HarmonyPriority(Priority.First)]
        private static void Postfix()
        {
            foreach (var type in Cards.Concat(MobPower.Types).Concat(OtherModels))
                ModelDb.Inject(type);

            _registered = true;
            LegacyLocalization.Load();
        }
    }

    [HarmonyPatch(typeof(LocManager), "TriggerLocaleChange")]
    private static class ChangeLanguage
    {
        private static void Prefix()
        {
            if (_registered) LegacyLocalization.Load();
        }
    }
}
