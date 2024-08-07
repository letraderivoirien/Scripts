using System;
using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace OscillatorsIndicators;

public class IndicatorDivergenceDetector : Indicator
{
    #region Parameters

    [InputParameter("Pivot lookback left offset", 20, 1, 9999, 1, 0)]
    public int Left = 5;

    [InputParameter("Pivot lookback right offset", 30, 1, 9999, 1, 0)]
    public int Right = 5;

    public LineOptions RegularBullishLineStyle
    {
        get => this.regularBullishLineStyle;
        private set
        {
            this.regularBullishLineStyle = value;
            this.regularBullishLinePen = ProcessPen(this.regularBullishLinePen, value);
        }
    }
    private LineOptions regularBullishLineStyle;
    private Pen regularBullishLinePen;

    public LineOptions HiddenBullishLineStyle
    {
        get => this.hiddenBullishLineStyle;
        private set
        {
            this.hiddenBullishLineStyle = value;
            this.hiddenBullishLinePen = ProcessPen(this.hiddenBullishLinePen, value);
        }
    }
    private LineOptions hiddenBullishLineStyle;
    private Pen hiddenBullishLinePen;

    public LineOptions RegularBearishLineStyle
    {
        get => this.regularBearishLineStyle;
        private set
        {
            this.regularBearishLineStyle = value;
            this.regularBearishLinePen = ProcessPen(this.regularBearishLinePen, value);
        }
    }
    private LineOptions regularBearishLineStyle;
    private Pen regularBearishLinePen;

    public LineOptions HiddenBearishLineStyle
    {
        get => this.hiddenBearishLineStyle;
        private set
        {
            this.hiddenBearishLineStyle = value;
            this.hiddenBearishLinePen = ProcessPen(this.hiddenBearishLinePen, value);
        }
    }
    private LineOptions hiddenBearishLineStyle;
    private Pen hiddenBearishLinePen;

    private Indicator subIndicator;
    private readonly List<int> bullishPatternIndexes;
    private readonly List<int> bearishPatternIndexes;

    private readonly IList<DivergenceRange> divergenceRanges;
    private readonly StringFormat centerNearSF;
    private Font currentFont;

    private string selectedIndicatorName;

    public override string SourceCodeLink => "https://github.com/Quantower/Scripts/blob/main/Indicators/IndicatorDivergenceDetector.cs";

    #endregion Parameters

    #region RSI parameters

    private int rsiPeriod;
    private PriceType rsiSourcePrice;
    private RSIMode rsiMode;
    public IndicatorCalculationType rsiCalculationType;

    #endregion RSI parameters

    #region MACD parameters

    public int macdFastPeriod;
    public int macdSlowPeriod;
    public IndicatorCalculationType macdCalculationType;

    #endregion MACD parameters

    #region AO parameters

    public int aoFastPeriod;
    public int aoSlowPeriod;

    #endregion AO parameters

    public IndicatorDivergenceDetector()
    {
        this.Name = "Divergence Detector";

        this.bullishPatternIndexes = new List<int>();
        this.bearishPatternIndexes = new List<int>();

        this.divergenceRanges = new List<DivergenceRange>();

        this.RegularBullishLineStyle = new LineOptions()
        {
            Color = Color.Green,
            Enabled = true,
            LineStyle = LineStyle.Solid,
            Width = 2,
            WithCheckBox = true,
            WithColor = true,
            WithNumeric = true
        };
        this.HiddenBullishLineStyle = new LineOptions()
        {
            Color = Color.Green,
            Enabled = false,
            LineStyle = LineStyle.DashDot,
            Width = 1,
            WithCheckBox = true,
            WithColor = true,
            WithNumeric = true
        };
        this.RegularBearishLineStyle = new LineOptions()
        {
            Color = Color.Red,
            Enabled = true,
            LineStyle = LineStyle.Solid,
            Width = 2,
            WithCheckBox = true,
            WithColor = true,
            WithNumeric = true
        };
        this.HiddenBearishLineStyle = new LineOptions()
        {
            Color = Color.Red,
            Enabled = false,
            LineStyle = LineStyle.DashDot,
            Width = 1,
            WithCheckBox = true,
            WithColor = true,
            WithNumeric = true
        };

        this.currentFont = new Font("Verdana", 10, GraphicsUnit.Pixel);
        this.centerNearSF = new StringFormat()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near
        };

        this.selectedIndicatorName = RSI;

        this.rsiPeriod = 14;
        this.rsiSourcePrice = PriceType.Close;
        this.rsiMode = RSIMode.Exponential;
        this.rsiCalculationType = Indicator.DEFAULT_CALCULATION_TYPE;

        this.macdFastPeriod = 12;
        this.macdSlowPeriod = 26;
        this.macdCalculationType = Indicator.DEFAULT_CALCULATION_TYPE;

        this.aoFastPeriod = 5;
        this.aoSlowPeriod = 34;
    }

    protected override void OnInit()
    {
        switch (this.selectedIndicatorName)
        {
            case RSI:
                {
                    this.subIndicator = Core.Instance.Indicators.BuiltIn.RSI(this.rsiPeriod, this.rsiSourcePrice, this.rsiMode, MaMode.SMA, 1, this.rsiCalculationType);
                    this.AddIndicator(this.subIndicator);
                    break;
                }
            case MACD:
                {
                    this.subIndicator = Core.Instance.Indicators.BuiltIn.MACD(this.macdFastPeriod, this.macdSlowPeriod, 1, this.rsiCalculationType);
                    this.AddIndicator(this.subIndicator);
                    break;
                }
            case "AO":
                {
                    this.subIndicator = Core.Instance.Indicators.BuiltIn.AwesomeOscillator(this.aoFastPeriod, this.aoSlowPeriod);
                    this.AddIndicator(this.subIndicator);
                    break;
                }
        }
    }
    protected override void OnUpdate(UpdateArgs args)
    {
        if (this.subIndicator == null)
            return;

        if (args.Reason == UpdateReason.NewTick)
            return;

        if (this.Count <= this.Left + this.Right && this.subIndicator?.Count <= this.Left + this.Right)
            return;

        bool hasBullishPattern = !double.IsNaN(PivotLow(this.subIndicator, this.Left, this.Right));
        bool hasBearishPattern = !double.IsNaN(PivotHigh(this.subIndicator, this.Left, this.Right));

        //
        // Regular and Hidden Bullish
        //
        if (hasBullishPattern)
        {
            if (this.bullishPatternIndexes.Count == 0 || this.bullishPatternIndexes[this.bullishPatternIndexes.Count - 1] != this.Count - 1)
                this.bullishPatternIndexes.Add(this.Count - 1);

            if (this.bullishPatternIndexes.Count > 2)
            {
                var prevPatternIndex = this.bullishPatternIndexes[this.bullishPatternIndexes.Count - 2];
                var prevIndicatorValue = this.subIndicator.GetValue(prevPatternIndex - this.Right, 0, SeekOriginHistory.Begin);
                var indicatorValue = this.subIndicator.GetValue(this.Right);
                var lowPrice = this.GetPrice(PriceType.Low, this.Right);
                var prevLowPrice = this.HistoricalData[prevPatternIndex - this.Right, SeekOriginHistory.Begin][PriceType.Low];

                var leftOffset = this.Count - prevPatternIndex + this.Right - 1;
                var rightOffset = this.Right;

                if (indicatorValue > prevIndicatorValue && lowPrice < prevLowPrice)
                    this.divergenceRanges.Add(new DivergenceRange(this.Time(leftOffset), prevLowPrice, this.Time(rightOffset), lowPrice, DivergenceType.RegularBullish));

                if (indicatorValue < prevIndicatorValue && lowPrice > prevLowPrice)
                    this.divergenceRanges.Add(new DivergenceRange(this.Time(leftOffset), prevLowPrice, this.Time(rightOffset), lowPrice, DivergenceType.HiddenBullish));
            }
        }

        //
        // Regular and Hidden Bearish
        //
        if (hasBearishPattern)
        {
            if (this.bearishPatternIndexes.Count == 0 || this.bearishPatternIndexes[this.bearishPatternIndexes.Count - 1] != this.Count - 1)
                this.bearishPatternIndexes.Add(this.Count - 1);

            if (this.bearishPatternIndexes.Count > 2)
            {
                var prevPatternIndex = this.bearishPatternIndexes[this.bearishPatternIndexes.Count - 2];
                var prevIndicatorValue = this.subIndicator.GetValue(prevPatternIndex - this
