using ExitGames.Client.Photon;

[System.Serializable]
public struct CharacterStats
{
    public int addMaxHp;
    public int addMaxArmor;
    public int addMoveSpeed;
    public float addWeaponDamageRate;
    public float addReduceDamageRate;
    public float addArmorReduceDamage;
    public float addExpRate;
    public float addScoreRate;
    public float addHpRecoveryRate;
    public float addArmorRecoveryRate;
    public float addDamageRateLeechHp;

    public static CharacterStats operator +(CharacterStats a, CharacterStats b)
    {
        var result = new CharacterStats();
        result.addMaxHp = a.addMaxHp + b.addMaxHp;
        result.addMaxArmor = a.addMaxArmor + b.addMaxArmor;
        result.addMoveSpeed = a.addMoveSpeed + b.addMoveSpeed;
        result.addWeaponDamageRate = a.addWeaponDamageRate + b.addWeaponDamageRate;
        result.addReduceDamageRate = a.addReduceDamageRate + b.addReduceDamageRate;
        result.addArmorReduceDamage = a.addArmorReduceDamage + b.addArmorReduceDamage;
        result.addExpRate = a.addExpRate + b.addExpRate;
        result.addScoreRate = a.addScoreRate + b.addScoreRate;
        result.addHpRecoveryRate = a.addHpRecoveryRate + b.addHpRecoveryRate;
        result.addArmorRecoveryRate = a.addArmorRecoveryRate + b.addArmorRecoveryRate;
        result.addDamageRateLeechHp = a.addDamageRateLeechHp + b.addDamageRateLeechHp;
        return result;
    }

    public static CharacterStats operator -(CharacterStats a, CharacterStats b)
    {
        var result = new CharacterStats();
        result.addMaxHp = a.addMaxHp - b.addMaxHp;
        result.addMaxArmor = a.addMaxArmor - b.addMaxArmor;
        result.addMoveSpeed = a.addMoveSpeed - b.addMoveSpeed;
        result.addWeaponDamageRate = a.addWeaponDamageRate - b.addWeaponDamageRate;
        result.addReduceDamageRate = a.addReduceDamageRate - b.addReduceDamageRate;
        result.addArmorReduceDamage = a.addArmorReduceDamage - b.addArmorReduceDamage;
        result.addExpRate = a.addExpRate - b.addExpRate;
        result.addScoreRate = a.addScoreRate - b.addScoreRate;
        result.addHpRecoveryRate = a.addHpRecoveryRate - b.addHpRecoveryRate;
        result.addArmorRecoveryRate = a.addArmorRecoveryRate - b.addArmorRecoveryRate;
        result.addDamageRateLeechHp = a.addDamageRateLeechHp - b.addDamageRateLeechHp;
        return result;
    }

    public static byte[] SerializeMethod(object customobject)
    {
        CharacterStats data = (CharacterStats)customobject;
        byte[] writeBytes = new byte[11 * 5];
        int index = 0;
        Protocol.Serialize(data.addMaxHp, writeBytes, ref index);
        Protocol.Serialize(data.addMaxArmor, writeBytes, ref index);
        Protocol.Serialize(data.addMoveSpeed, writeBytes, ref index);
        Protocol.Serialize(data.addWeaponDamageRate, writeBytes, ref index);
        Protocol.Serialize(data.addReduceDamageRate, writeBytes, ref index);
        Protocol.Serialize(data.addArmorReduceDamage, writeBytes, ref index);
        Protocol.Serialize(data.addExpRate, writeBytes, ref index);
        Protocol.Serialize(data.addScoreRate, writeBytes, ref index);
        Protocol.Serialize(data.addHpRecoveryRate, writeBytes, ref index);
        Protocol.Serialize(data.addArmorRecoveryRate, writeBytes, ref index);
        Protocol.Serialize(data.addDamageRateLeechHp, writeBytes, ref index);
        return writeBytes;
    }

    public static object DeserializeMethod(byte[] readBytes)
    {
        CharacterStats data = new CharacterStats();
        int index = 0;
        int tempInt = 0;
        float tempFloat = 0f;
        Protocol.Deserialize(out tempInt, readBytes, ref index);
        data.addMaxHp = tempInt;
        Protocol.Deserialize(out tempInt, readBytes, ref index);
        data.addMaxArmor = tempInt;
        Protocol.Deserialize(out tempInt, readBytes, ref index);
        data.addMoveSpeed = tempInt;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addWeaponDamageRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addReduceDamageRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addArmorReduceDamage = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addExpRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addScoreRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addHpRecoveryRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addArmorRecoveryRate = tempFloat;
        Protocol.Deserialize(out tempFloat, readBytes, ref index);
        data.addDamageRateLeechHp = tempFloat;
        return data;
    }
}
