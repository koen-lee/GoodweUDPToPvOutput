<?php

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class GoodWeConnector
{
    const BROADCAST_MESSAGE = 'aa55c07f0102000241';
    const USAGE_MESSAGE = '7f0375940049d5c2';
    const INFO_MESSAGE = '7f03753100280409';
    const PORT = 8899;

    protected $socket;


    public function __construct()
    {
        if (!($this->socket = socket_create(AF_INET, SOCK_DGRAM, SOL_UDP))) {
            $errorcode = socket_last_error();
            $errormsg = socket_strerror($errorcode);

            die("Couldn't create socket: [$errorcode] $errormsg \n");
        }
        socket_set_option($this->socket,SOL_SOCKET, SO_RCVTIMEO, ['sec' => 1, 'usec' => 0]);
    }

    public function sendMessage($message, $ip, $port = self::PORT)
    {
        $message = hex2bin($message);
        if (!socket_sendto($this->socket, $message, strlen($message), 0, $ip, $port)) {
            $errorcode = socket_last_error();
            $errormsg = socket_strerror($errorcode);

            die("Could not send data: [$errorcode] $errormsg \n");
        }

        if (socket_recv($this->socket,$reply, 2048, MSG_WAITALL) === FALSE) {
            $errorcode = socket_last_error();
            $errormsg = socket_strerror($errorcode);

            die("Could not receive data: [$errorcode] $errormsg \n");
        }

        return $reply;
    }

    public function sendUsageMessage($ip)
    {
        return $this->sendMessage(self::USAGE_MESSAGE, $ip);
    }

    public function getSerial($ip)
    {
        return $this->sendMessage(self::INFO_MESSAGE, $ip);
    }
}
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////
$inverters = [
    [
        'name' => 'GW8K-DT',
        'ip' => '192.168.77.34',
        // No pvoutput defined
    ],
];
////voor goodwe info//////////////////////////////////////////////////////////////////////////////////////////////////////////////
class GoodWeInfo
{
    protected $serialNumber;
    protected $inverterType;
    protected $dsp1version;
    protected $dsp2version;
    protected $armVersion;

    public function __construct($serialReply)
    {
        GoodWeValidator::validate($serialReply);

        $this->serialReply = $serialReply;

        $this->serialNumber = substr($serialReply, 11, 16);
        $this->inverterType = substr($serialReply, 27, 9);

        $message = bin2hex($serialReply);
        $this->dsp1version = hexdec(substr($message, 142, 4));
        $this->dsp2version = hexdec(substr($message, 146, 4));
        $this->armVersion = hexdec(substr($message, 150, 4));
    }

    public function show()
    {
        echo 'Inverter information: ' . PHP_EOL;
        echo 'Serial Number: ' . $this->serialNumber . PHP_EOL;
        echo 'Inverter Type: ' . $this->inverterType . PHP_EOL;

        echo 'Version: DSP1: ' . $this->dsp1version . PHP_EOL;
        echo 'Version: DSP2: ' . $this->dsp2version . PHP_EOL;
        echo 'Version: ARM:  ' . $this->armVersion . PHP_EOL;
    }
}
//////////voor de live data ////////////////////////////////////////////////////////////////////////////////////////////////////////
final class GoodWeProcessor
{
    protected $binary;

    public static function process($binary)
    {
        GoodWeValidator::validate($binary);
        $date = self::getDateTime($binary);

        $goodweOutput = new GoodWeOutput();
        $goodweOutput->setDateTime($date);
        $goodweOutput->setVoltDc1(hexdec(bin2hex($binary[11] . $binary[12])) / 10);
        $goodweOutput->setCurrentDc1(hexdec(bin2hex($binary[13]. $binary[14])) / 10);
        $goodweOutput->setVoltDc2(hexdec(bin2hex($binary[15] . $binary[16])) / 10);
        $goodweOutput->setCurrentDc2(hexdec(bin2hex($binary[17]. $binary[18])) / 10);
        $goodweOutput->setVoltAc1(hexdec(bin2hex($binary[41] . $binary[42])) / 10);
        $goodweOutput->setVoltAc2(hexdec(bin2hex($binary[43] . $binary[44])) / 10);
        $goodweOutput->setVoltAc3(hexdec(bin2hex($binary[45] . $binary[46])) / 10);
        $goodweOutput->setCurrentAc1(hexdec(bin2hex($binary[47] . $binary[48])) / 10);
        $goodweOutput->setCurrentAc2(hexdec(bin2hex($binary[49] . $binary[50])) / 10);
        $goodweOutput->setCurrentAc3(hexdec(bin2hex($binary[51] . $binary[52])) / 10);
        $goodweOutput->setFrequencyAc1(hexdec(bin2hex($binary[53] . $binary[54])) / 100);
        $goodweOutput->setFrequencyAc2(hexdec(bin2hex($binary[55] . $binary[56])) / 100);
        $goodweOutput->setFrequencyAc3(hexdec(bin2hex($binary[57] . $binary[58])) / 100);
        $goodweOutput->setPower(hexdec(bin2hex($binary[61] . $binary[62])) / 1000);
        $goodweOutput->setWorkMode(hexdec(bin2hex($binary[63] . $binary[64])));
        $goodweOutput->setTemperature(hexdec(bin2hex($binary[87] . $binary[88])) / 10);
        $goodweOutput->setGenerationToday(hexdec(bin2hex($binary[93] . $binary[94])) / 10);
        $goodweOutput->setGenerationTotal(hexdec(bin2hex($binary[95] . $binary[96] . $binary[97] . $binary[98])) / 10);
        $goodweOutput->setTotalHours(hexdec(bin2hex($binary[99] . $binary[100] . $binary[101] . $binary[102])));
        $goodweOutput->setSafetyCode(hexdec(bin2hex($binary[103] . $binary[104])) / 100);
        $goodweOutput->setRSSI(hexdec(bin2hex($binary[149] . $binary[150])));

        return $goodweOutput;

    }

    public static function getDateTime($binary): \DateTime
    {
        $date = \DateTime::createFromFormat(
            'ymdHis',
            str_pad(hexdec(bin2hex($binary[5])), 2, '0', STR_PAD_LEFT) .
            str_pad(hexdec(bin2hex($binary[6])), 2, '0', STR_PAD_LEFT) .
            str_pad(hexdec(bin2hex($binary[7])), 2, '0', STR_PAD_LEFT) .
            str_pad(hexdec(bin2hex($binary[8])), 2, '0', STR_PAD_LEFT) .
            str_pad(hexdec(bin2hex($binary[9])), 2, '0', STR_PAD_LEFT) .
            str_pad(hexdec(bin2hex($binary[10])), 2, '0', STR_PAD_LEFT)
        );
        if ($date === false) {
            throw new \Exception("Invalid date time");
        }

        return $date;
    }
}
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////
final class GoodWeOutput
{
    const WORK_MODE_WAIT = 0;
    const WORK_MODE_NORMAL = 1;
    const WORK_MODE_ERROR = 2;
    const WORK_MODE_CHECK = 4;


        protected $voltDc1;
        protected $currentDc1;
        protected $voltDc2;
        protected $currentDc2;
        protected $voltAc1;
        protected $voltAc2;
        protected $voltAc3;
        protected $currentAc1;
        protected $currentAc2;
        protected $currentAc3;
        protected $frequencyAc1;
        protected $frequencyAc2;
        protected $frequencyAc3;
        protected $power;
        protected $workMode;
        protected $temperature;
        protected $generationTotal;
        protected $generationToday;
        protected $totalHours;
        protected $setSafetyCode;
        protected $rssi;



    /**
     * @var \DateTime
     */
    private $dateTime;

    public function __construct()
    {
    }

    public function setDateTime($dateTime): self
    {
        $this->dateTime = $dateTime;

        return $this;
    }

    public function getDateTime(): \DateTime
    {
        return $this->dateTime;
    }

    public function toArray(): array
    {
        return [
            'dateTime' => $this->dateTime->format(DATE_ATOM),
            'voltDc1' => $this->voltDc1,
            'currentDc1' => $this->currentDc1,
            'voltDc2' => $this->voltDc2,
            'currentDc2' => $this->currentDc2,
            'voltAc1' => $this->voltAc1,
            'voltAc2' => $this->voltAc2,
            'voltAc3' => $this->voltAc3,
            'currentAc1' => $this->currentAc1,
            'currentAc2' => $this->currentAc2,
            'currentAc3' => $this->currentAc3,
            'frequencyAc1' => $this->frequencyAc1,
            'frequencyAc2' => $this->frequencyAc2,
            'frequencyAc3' => $this->frequencyAc3,
            'power' => $this->power,
            'workMode' => $this->workMode,
            'readableWorkMode' => $this->getReadableWorkMode(),
            'temperature' => $this->temperature,
            'generationToday' => $this->generationToday,
            'generationTotal' => $this->generationTotal,
            'totalHours' => $this->totalHours,
            'SafetyCode' => $this->SafetyCode,
            'rssi' => $this->rssi,
        ];
    }

    public function setVoltDc1($voltDc1)
    {
        $this->voltDc1 = $voltDc1;
    }

    public function setVoltDc2($voltDc2)
    {
        $this->voltDc2 = $voltDc2;
    }

    public function setCurrentDc1($currentDc1)
    {
        $this->currentDc1 = $currentDc1;
    }

    public function setCurrentDc2($currentDc2)
    {
        $this->currentDc2 = $currentDc2;
    }

    public function setVoltAc1($voltAc1)
    {
        $this->voltAc1 = $voltAc1;
    }
    public function getVoltageAc1()
    {
        return $this->voltAc1;
    }

    public function setVoltAc2($voltAc2)
    {
        $this->voltAc2 = $voltAc2;
    }
    public function getVoltageAc2()
    {
        return $this->voltAc2;
    }

    public function setVoltAc3($voltAc3)
    {
        $this->voltAc3 = $voltAc3;
    }
    public function getVoltageAc3()
    {
        return $this->voltAc3;
    }

    public function setCurrentAc1($currentAc1)
    {
        $this->currentAc1 = $currentAc1;
    }

    public function setCurrentAc2($currentAc2)
    {
        $this->currentAc2 = $currentAc2;
    }

    public function setCurrentAc3($currentAc3)
    {
        $this->currentAc3 = $currentAc3;
    }

    public function setFrequencyAc1($frequencyAc1)
    {
        $this->frequencyAc1 = $frequencyAc1;
    }

    public function setFrequencyAc2($frequencyAc2)
    {
        $this->frequencyAc2 = $frequencyAc2;
    }

    public function setFrequencyAc3($frequencyAc3)
    {
        $this->frequencyAc3 = $frequencyAc3;
    }

    public function setPower($power)
    {
        $this->power = $power;
    }

    public function getPower()
    {
        return $this->power;
    }

    public function setWorkMode($workMode)
    {
        $this->workMode = $workMode;
    }

    public function setTemperature($temperature)
    {
        $this->temperature = $temperature;
    }

    public function getTemperature()
    {
        return $this->temperature;
    }

    public function setGenerationToday($today)
    {
        $this->generationToday = $today;
    }

    public function getGenerationToday()
    {
        return $this->generationToday;
    }

    public function setGenerationTotal($generationTotal)
    {
        $this->generationTotal = $generationTotal;
    }

    public function setTotalHours($totalHours)
    {
        $this->totalHours = $totalHours;
    }

    public function setSafetyCode($SafetyCode)
    {
        $this->SafetyCode = $SafetyCode;
    }

    public function setRSSI($rssi)
    {
        $this->rssi = $rssi;
    }

    private function getReadableWorkMode()
    {
        $workModes = [
            self::WORK_MODE_WAIT => 'Wait',
            self::WORK_MODE_NORMAL => 'Normal',
            self::WORK_MODE_ERROR => 'Error',
            self::WORK_MODE_CHECK => 'Check',
        ];

        return $workModes[$this->workMode];
    }

    public function show()
    {
        echo 'GoodWe output from ' . $this->dateTime->format(DATE_ISO8601) . PHP_EOL;

        echo 'DC1 Voltage   ' . $this->voltDc1 . 'V' . PHP_EOL;
        echo 'DC1 Current   ' . $this->currentDc1 . 'A' . PHP_EOL;
        echo 'DC2 Voltage   ' . $this->voltDc2 . 'V' . PHP_EOL;
        echo 'DC2 Current   ' . $this->currentDc2 . 'A' . PHP_EOL;
        echo 'AC1 Voltage   ' . $this->voltAc1 . 'V' . PHP_EOL;
        echo 'AC2 Voltage   ' . $this->voltAc2 . 'V' . PHP_EOL;
        echo 'AC3 Voltage   ' . $this->voltAc3 . 'V' . PHP_EOL;
        echo 'AC1 Current   ' . $this->currentAc1 . 'A' . PHP_EOL;
        echo 'AC2 Current   ' . $this->currentAc2 . 'A' . PHP_EOL;
        echo 'AC3 Current   ' . $this->currentAc3 . 'A' . PHP_EOL;
        echo 'AC1 Frequency ' . $this->frequencyAc1 . 'Hz' . PHP_EOL;
        echo 'AC2 Frequency ' . $this->frequencyAc2 . 'Hz' . PHP_EOL;
        echo 'AC3 Frequency ' . $this->frequencyAc3 . 'Hz' . PHP_EOL;
        echo 'Power         ' . $this->power . 'kW'. PHP_EOL;
        echo 'WorkMode      ' . $this->workMode . PHP_EOL;
        echo 'WorkMode      ' . $this->getReadableWorkMode() . PHP_EOL;
        echo 'temperature   ' . $this->temperature . '°C' . PHP_EOL;
        echo 'Energy Today  ' . $this->generationToday . 'kWh' . PHP_EOL;
        echo 'Energy Total  ' . $this->generationTotal . 'kWh' . PHP_EOL;
        echo 'Total hours   ' . $this->totalHours . 'h' . PHP_EOL;
        echo 'SafetyCode    ' . $this->SafetyCode . '-' . PHP_EOL;
        echo 'WiFi RSSI     ' . $this->rssi . '%' . PHP_EOL;
    }
}
////////GoodWeValidator//////////////////////////////////////////////////////////////////////////////////////////////////////////
class GoodWeValidator
{
    public static function validate($binary)
    {
        $hex = bin2hex($binary);

        $crc = bin2hex(substr($binary, -1)) . bin2hex(substr($binary, -2, 1));
        $payload = substr($binary, 2, strlen($binary) - 4);
        $calculatedCrc = self::calculateCrc($payload);
        if ($crc !== $calculatedCrc) {
            throw new \Exception('Invalid CRC, got ' . $crc . ', but calculated ' . $calculatedCrc);
        }

        if (bin2hex($binary[0] . $binary[1]) !== 'aa55') {
            throw new \Exception("Invalid header in inverter response. Expected aa55, got " . substr($hex, 0, 4));
        }
    }

    private static function calculateCrc($payload)
    {
        $crc = 0xFFFF;
        $odd = null;

        for ($i = 0; $i < strlen($payload); $i++)
        {
            $crc = $crc ^ hexdec(bin2hex($payload[$i]));

            for ($j = 0; $j < 8; $j++)
            {
                $odd = ($crc & 0x0001) != 0;
                $crc >>= 1;
                if ($odd)
                {
                    $crc ^= 0xA001;
                }
            }
        }
        return str_pad(dechex($crc), 4, '0', STR_PAD_LEFT);
    }
}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
$connector = new GoodWeConnector();

foreach ($inverters as $inverter) {
    echo "===========================" . PHP_EOL;
    echo "Trying " . $inverter['name'] . ' (' . $inverter['ip'] . ')' . PHP_EOL;

    $serialReply = $connector->getSerial($inverter['ip']);

    $goodWeInfo = new GoodWeInfo($serialReply);
    $goodWeInfo->show();

    $reply = $connector->sendUsageMessage($inverter['ip']);
    $goodweOutput = GoodWeProcessor::process($reply);
    $goodweOutput->show();
}
