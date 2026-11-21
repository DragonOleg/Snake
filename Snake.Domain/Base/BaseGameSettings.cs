using System;

namespace Snake.Domain
{
    /// <summary>Параметры одной партии. Значения по умолчанию подобраны под терминал 80x30.</summary>
    public class BaseGameSettings
    {
        public int BoardWidth { get; set; } = 30;

        public int BoardHeight { get; set; } = 20;

        public int InitialLength { get; set; } = 3;

        /// <summary>Задержка кадра на первом уровне, мс.</summary>
        public int BaseFrameDelayMs { get; set; } = 180;

        /// <summary>Минимальная задержка кадра (максимальная скорость), мс.</summary>
        public int MinFrameDelayMs { get; set; } = 60;

        /// <summary>На сколько миллисекунд сокращается кадр с каждым уровнем.</summary>
        public int SpeedStepMs { get; set; } = 12;

        /// <summary>Сколько еды нужно съесть для повышения уровня.</summary>
        public int FoodPerLevel { get; set; } = 5;

        public int ScorePerFood { get; set; } = 10;

        /// <summary>Сколько препятствий добавляется за каждый новый уровень.</summary>
        public int ObstaclesPerLevel { get; set; } = 2;

        public int MaxObstacles { get; set; } = 40;

        public void Validate()
        {
            if (BoardWidth < 10 || BoardHeight < 10)
            {
                throw new ArgumentOutOfRangeException(nameof(BoardWidth), "Поле должно быть не меньше 10x10.");
            }

            if (InitialLength < 1 || InitialLength >= BoardWidth)
            {
                throw new ArgumentOutOfRangeException(nameof(InitialLength), "Начальная длина не помещается на поле.");
            }

            if (BaseFrameDelayMs <= MinFrameDelayMs || MinFrameDelayMs < 10)
            {
                throw new ArgumentOutOfRangeException(nameof(BaseFrameDelayMs), "Некорректные задержки кадра.");
            }

            if (FoodPerLevel < 1 || ScorePerFood < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(FoodPerLevel), "Некорректные очки или шаг уровня.");
            }
        }
    }
}
