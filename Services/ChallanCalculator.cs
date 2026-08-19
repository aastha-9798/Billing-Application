using System;
using System.Collections.Generic;
using System.Text;

using PlateBilling.Models;

namespace PlateBilling.Services;

public class ChallanCalculator
{
    public decimal CalculateTotal(
        bool areaBilling,
        ClientPlateRate? clientPlateRate,
        PlateType plateType,
        int quantity,
        decimal globalAreaRate)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        if (areaBilling)
        {
            return plateType.Length
                * plateType.Breadth
                * quantity
                * globalAreaRate;
        }

        if (clientPlateRate == null)
        {
            throw new InvalidOperationException(
                "No rate is configured for this client and plate type.");
        }

        return clientPlateRate.Rate * quantity;
    }


}
