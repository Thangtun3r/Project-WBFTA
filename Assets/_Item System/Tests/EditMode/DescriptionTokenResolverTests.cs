using NUnit.Framework;
using UnityEngine;

public class DescriptionTokenResolverTests
{
    [Test]
    public void Resolve_ItemDefinition_FormatsExplicitTokensAndStackValues()
    {
        ItemDefinition definition = CreateItemDefinition(
            "Chance {item:ProcChance|percent}; damage {item:DamageMultiplier|x}; speed {param:LaunchSpeed}.");
        definition.itemStats.Add(new ItemStatEntry
        {
            statType = ItemStatType.ProcChance,
            baseValue = 0.2f,
            perStackValue = 0.1f
        });
        definition.itemStats.Add(new ItemStatEntry
        {
            statType = ItemStatType.DamageMultiplier,
            baseValue = 2f,
            perStackValue = 0.5f
        });
        definition.parameters.Add(new ItemParameterEntry
        {
            key = "LaunchSpeed",
            baseValue = 6f,
            perStackValue = 2f
        });

        Assert.AreEqual("Chance 40%; damage 3x; speed 10.", DescriptionTokenResolver.Resolve(definition, 3));
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void Resolve_ItemDefinition_SupportsShortAliasesAndPlayerContributions()
    {
        ItemDefinition definition = CreateItemDefinition("Crit {CritChance|percent}; radius {Radius}.");
        definition.playerStats.Add(new PlayerStatEntry
        {
            statType = PlayerStatType.CritChance,
            baseValue = 0.2f,
            perStackValue = 0.1f
        });
        definition.itemStats.Add(new ItemStatEntry
        {
            statType = ItemStatType.Radius,
            baseValue = 10f,
            perStackValue = 5f
        });

        Assert.AreEqual("Crit 30%; radius 15.", DescriptionTokenResolver.Resolve(definition, 2));
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void Resolve_UnresolvedToken_RemainsVisibleAndFailsValidation()
    {
        ItemDefinition definition = CreateItemDefinition("Unknown {item:MissingStat}.");

        Assert.AreEqual("Unknown {item:MissingStat}.", DescriptionTokenResolver.Resolve(definition));
        Assert.IsFalse(DescriptionTokenResolver.Validate(definition));
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void Resolve_UnmatchedBrace_RemainsVisibleAndFailsValidation()
    {
        ItemDefinition definition = CreateItemDefinition("Unknown {item:ProcChance.");

        Assert.AreEqual("Unknown {item:ProcChance.", DescriptionTokenResolver.Resolve(definition));
        Assert.IsFalse(DescriptionTokenResolver.Validate(definition));
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void Resolve_ItemRuntime_AppliesAttachedModifierToLiveItemStat()
    {
        GameObject owner = new GameObject("DescriptionTokenResolverTests");
        PlayerInventory inventory = owner.AddComponent<PlayerInventory>();
        ItemDefinition itemDefinition = CreateItemDefinition("Damage {item:DamageMultiplier|x}.");
        itemDefinition.itemStats.Add(new ItemStatEntry
        {
            statType = ItemStatType.DamageMultiplier,
            baseValue = 2f,
            perStackValue = 0f
        });

        ModifierDefinition modifierDefinition = ScriptableObject.CreateInstance<ModifierDefinition>();
        modifierDefinition.itemStatModifiers.Add(new ItemStatModifierEntry
        {
            statType = ItemStatType.DamageMultiplier,
            multiplierBonus = 0.5f
        });

        ItemRuntime runtime = new ItemRuntime();
        runtime.Initialize(itemDefinition, null, owner, inventory.ItemContext);
        runtime.AttachModifier(modifierDefinition, null);

        Assert.AreEqual("Damage 3x.", DescriptionTokenResolver.Resolve(runtime));

        Object.DestroyImmediate(modifierDefinition);
        Object.DestroyImmediate(itemDefinition);
        Object.DestroyImmediate(owner);
    }

    [Test]
    public void Resolve_ModifierDefinition_ResolvesParametersAndProcCoefficient()
    {
        ModifierDefinition definition = ScriptableObject.CreateInstance<ModifierDefinition>();
        definition.description = "Chance {modifier:procCoefficient|percent}; scale {Dice.OddMultiplier|x}.";
        definition.procCoefficient = 0.25f;
        definition.parameters.Add(new ItemParameterEntry
        {
            key = "Dice.OddMultiplier",
            baseValue = 0.5f
        });

        Assert.AreEqual("Chance 25%; scale 0.5x.", DescriptionTokenResolver.Resolve(definition));
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void Resolve_ModifierRuntime_ResolvesAttachedStackDerivedFields()
    {
        GameObject owner = new GameObject("DescriptionTokenResolverTests");
        PlayerInventory inventory = owner.AddComponent<PlayerInventory>();
        ItemDefinition itemDefinition = CreateItemDefinition("Test");
        ModifierDefinition modifierDefinition = ScriptableObject.CreateInstance<ModifierDefinition>();
        modifierDefinition.procCoefficient = 0.1f;
        modifierDefinition.description =
            "Total {modifier:procCoefficientTotal|percent}; per stack {modifier:procCoefficientPerStack|percent}.";

        ItemRuntime runtime = new ItemRuntime();
        runtime.Initialize(itemDefinition, null, owner, inventory.ItemContext);
        runtime.AddStack(2);

        ModifierRuntime modifierRuntime = runtime.AttachModifier(modifierDefinition, null);

        Assert.AreEqual(
            "Total 30%; per stack 10%.",
            DescriptionTokenResolver.Resolve(modifierRuntime));

        Object.DestroyImmediate(modifierDefinition);
        Object.DestroyImmediate(itemDefinition);
        Object.DestroyImmediate(owner);
    }

    private static ItemDefinition CreateItemDefinition(string description)
    {
        ItemDefinition definition = ScriptableObject.CreateInstance<ItemDefinition>();
        definition.itemId = "TestItem";
        definition.description = description;
        return definition;
    }
}
