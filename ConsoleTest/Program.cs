using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.Minecraft.Server;
using OneLauncher.Core.Serialization;
using System.Text.Json;

/// <summary>
/// 表示求解结果，包含 y 值和对应的 x 值。
/// </summary>
public struct Solution
{
    public double Y { get; }
    public double X { get; }

    public Solution(double y, double x)
    {
        Y = y;
        X = x;
    }

    public override string ToString()
    {
        return $"For y = {Y:F2}, x = {X:F6}"; 
    }
}

/// <summary>
/// 根据给定的 z 值和 y 的特定范围计算 x。
/// 方程式: 0.1 * 58.99 * x / 1000 / y * 100 = z
/// </summary>
public class EquationSolver
{
    private const double Factor = 0.5899; 

    public List<Solution> SolveForX(double z)
    {
        List<Solution> solutions = new List<Solution>();
        double[] yValues = { 1.99, 2.00, 2.01 }; // y 的可能值

        foreach (double y in yValues)
        {
            // x = (z * y) / 0.5899
            double x = (z * y) / Factor;
            solutions.Add(new Solution(y, x));
        }

        return solutions;
    }
}