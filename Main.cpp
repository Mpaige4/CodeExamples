// Fill out your copyright notice in the Description page of Project Settings.


#include "Main.h"
#include "GameFramework/SpringArmComponent.h"
#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "GameFramework/CharacterMovementComponent.h"


// Sets default values
AMain::AMain()
{
 	// Set this character to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	/*Create camera boom (pulls towards the player if there is collision)*/
	CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
	CameraBoom->SetupAttachment(GetRootComponent());
	CameraBoom->TargetArmLength = 400.f; //camera follows at this distance
	CameraBoom->bUsePawnControlRotation = true; //rotate arm based on controller
	
	//set size for collision capsule
	GetCapsuleComponent()->SetCapsuleSize(27.1f, 92.2f);

	//create follow camera
	FollowCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FollowCamera"));
	FollowCamera->SetupAttachment(CameraBoom, USpringArmComponent::SocketName);
	//attach the camera to the end of the boom and let the boom adjust the match
	//the controller rotation
	FollowCamera->bUsePawnControlRotation = false;

	//set our turn rates for input
	BaseTurnRate = 65.f;
	BaseLookUpRate = 65.f;
	

	//don't rotate when the controller rotates
	//only want to rotate camera
	bUseControllerRotationYaw = false;
	bUseControllerRotationPitch = false;
	bUseControllerRotationRoll = false;


	//configure character movement
	GetCharacterMovement()->bOrientRotationToMovement = true; //character moves in the direction of input...
	GetCharacterMovement()->RotationRate = FRotator(0.0f, 540.f, 0.0f); //... At this rotation rate
	GetCharacterMovement()->JumpZVelocity = 650.f;
	GetCharacterMovement()->AirControl = 0.3f;

	MaxHealth = 100.f;
	Health = MaxHealth;
	MaxStamina = 200.f;
	Stamina = MaxStamina;
	Coins = 0;
	


}

// Called when the game starts or when spawned
void AMain::BeginPlay()
{
	Super::BeginPlay();
	
}

// Called every frame
void AMain::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}

// Called to bind functionality to input
void AMain::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);

	check(PlayerInputComponent);

	PlayerInputComponent->BindAction("Jump", IE_Pressed, this, &ACharacter::Jump);
	PlayerInputComponent->BindAction("Jump", IE_Released, this, &ACharacter::StopJumping);
	
	PlayerInputComponent->BindAxis("MoveForward", this, &AMain::MoveForward);
	PlayerInputComponent->BindAxis("MoveRight", this, &AMain::MoveRight);

	PlayerInputComponent->BindAxis("Turn", this, &APawn::AddControllerYawInput);
	PlayerInputComponent->BindAxis("LookUp", this, &APawn::AddControllerPitchInput);

	PlayerInputComponent->BindAxis("TurnRate", this, &AMain::TurnAtRate);
	PlayerInputComponent->BindAxis("LookUpRate", this, &AMain::LookUpAtRate);


}

void AMain::MoveForward(float Value)
{

	if (Controller != nullptr && Value != 0.0f)
	{
		//find which way is forward
		const FRotator Rotation = Controller->GetControlRotation();
		const FRotator YawRotation(0.0f, Rotation.Yaw, 0.0f);

		const FVector Direction = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::X);
		AddMovementInput(Direction, Value);

	}

}
void AMain::MoveRight(float Value)
{

	if (Controller != nullptr && Value != 0.0f)
	{
		//find which way is forward
		const FRotator Rotation = Controller->GetControlRotation();
		const FRotator YawRotation(0.0f, Rotation.Yaw, 0.0f);

		const FVector Direction = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::Y);
		AddMovementInput(Direction, Value);

	}

}

void AMain::TurnAtRate(float Rate)
{
	AddControllerYawInput(Rate * BaseTurnRate * GetWorld()->GetDeltaSeconds());
}


void AMain::LookUpAtRate(float Rate)
{
	AddControllerPitchInput(Rate * BaseLookUpRate * GetWorld()->GetDeltaSeconds());
}

void AMain::IncreaseHealth(float Amount)
{
	if (Health != MaxHealth)
	{
		Health += Amount;
	}
	
}


void AMain::DecreaseHealth(float Amount)
{


	if (Health - Amount <= 0.f)
	{
		Health -= Amount;
		Die();
	}
	else
	{
		Health -= Amount;
	}

}

void AMain::Die()
{

}


void AMain::IncreaseCoins(int32 Amount)
{
	Coins += Amount;

}