using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DireccionInput
{
    Null,
    Arriba,
    Izquierda,
    Derecha,
    Abajo
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager; //Se hizo la referencia en la clase arrastrando el GameManager al PlayerController Script

    [Header("Config")]
    [SerializeField] private float velocidadMovimiento;
    [SerializeField] private float valorsalto = 15f;
    [SerializeField] private float gravedad = 20f;

    [Header("Carril")]
    [SerializeField] private float posicionCarrilIzq = -3.1f;
    [SerializeField] private float posicionCarrilDer = 3.1f;

    public bool EstaSaltando { get; private set; } //propiedad. escribes prop + TAB
    public bool EstaDeslizando { get; private set; }

    private DireccionInput direccionInput;
    private Coroutine coroutineDeslizar;
    private CharacterController characterController;
    //referencia del PlayerAnimations. Funciona asi porque ambas clases estan en el mismo GameObject
    private PlayerAnimations playerAnimaciones;

    private float posicionVertical;
    private int carrilActual;
    private Vector3 direccionDeseada;

    private float controllerRadio;
    private float controllerAltura;
    private float controllerCenterY;


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerAnimaciones = GetComponent<PlayerAnimations>(); 
    }

    // Start is called before the first frame update
    void Start()
    {
        controllerRadio = characterController.radius;
        controllerAltura = characterController.height;
        controllerCenterY = characterController.center.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager.EstadoActual == EstadosDelJuego.Inicio ||
            gameManager.EstadoActual == EstadosDelJuego.GameOver)
        {
            return;
        }

        DetectarInput();
        ControlarCarriles();
        CalcularMovimientoVertical();
        MoverPersonaje();
    }

    private void MoverPersonaje()
    {
        Vector3 nuevaPos = new Vector3(direccionDeseada.x, posicionVertical, velocidadMovimiento);
        characterController.Move(nuevaPos * Time.deltaTime);
    }

    private void CalcularMovimientoVertical()
    {
        if (characterController.isGrounded)
        {
            EstaSaltando = false;
            posicionVertical = 0f;

            if (EstaDeslizando == false && EstaSaltando == false)
            {
                playerAnimaciones.MostrarAnimacionCorrer(); //animacion
            }

            if (direccionInput == DireccionInput.Arriba)
            {
                SoundManager.Instancia.ReproducirSonidoFX(SoundManager.Instancia.saltoClip);
                EstaSaltando = true;
                posicionVertical = valorsalto;
                playerAnimaciones.MostrarAnimacionSaltar(); //animacion
                if (coroutineDeslizar != null)
                {
                    StopCoroutine(coroutineDeslizar);
                    EstaDeslizando = false;
                    ModificarColliderDeslizar(false);
                }
            }

            if (direccionInput == DireccionInput.Abajo)
            {
                if (EstaDeslizando)
                {
                    return;
                }

                if (coroutineDeslizar != null)
                {
                    StopCoroutine(coroutineDeslizar);
                }

                DeslizarPersonaje();
            }
        }
        else
        {
            if (direccionInput == DireccionInput.Abajo)
            {
                posicionVertical -= valorsalto;
                DeslizarPersonaje();
            }
        }

        posicionVertical -= gravedad * Time.deltaTime; //tamb funciona si le pones else
        //Debug.Log("posVert: " + posicionVertical);
    }

    private void DeslizarPersonaje()
    {
        coroutineDeslizar = StartCoroutine(CODeslizarPersonaje());
    }

    private IEnumerator CODeslizarPersonaje()
    {
        SoundManager.Instancia.ReproducirSonidoFX(SoundManager.Instancia.deslizarClip);
        EstaDeslizando = true;
        playerAnimaciones.MostrarAnimacionDeslizar(); //animacion
        ModificarColliderDeslizar(true);
        yield return new WaitForSeconds(2f);
        //
        EstaDeslizando = false;
        ModificarColliderDeslizar(false);
    }

    private void ModificarColliderDeslizar(bool modificar)
    {
        if (modificar)
        {
            //modificar collider con valores pequeños
            characterController.radius = 0.3f;
            characterController.height = 0.6f;
            characterController.center = new Vector3(0f, 0.35f, 0f);
        }
        else
        {
            characterController.radius = controllerRadio;
            characterController.height = controllerAltura;
            characterController.center = new Vector3(0f, controllerCenterY, 0f);
        }
    }

    private void ControlarCarriles()
    {
        switch (carrilActual)
        {
            case -1:
                LogicaCarrilIzq();
                break;
            case 0:
                LogicaCarrilCentral();
                break;
            case 1:
                LogicaCarrilDer();
                break;
        }
    }

    private void LogicaCarrilCentral()
    {
        if (transform.position.x > 0.1f)
        {
            MoverHorizontal(0f, Vector3.left);
        }
        else if (transform.position.x < -0.1f)
        {
            MoverHorizontal(0f, Vector3.right);
        }
        else //si no lo pones el player vibra
        {
            direccionDeseada = Vector3.zero;
        }
    }

    private void LogicaCarrilIzq()
    {
        MoverHorizontal(posicionCarrilIzq, Vector3.left);
    }

    private void LogicaCarrilDer()
    {
        MoverHorizontal(posicionCarrilDer, Vector3.right);
    }

    private void MoverHorizontal(float posicionX, Vector3 dirMovimiento)
    {
        float posicionHorizontal = Mathf.Abs(transform.position.x - posicionX);
        if (posicionHorizontal > 0.1f)
        {
            direccionDeseada = Vector3.Lerp(direccionDeseada, dirMovimiento * 20f, Time.deltaTime * 500);
        }
        else
        {
            direccionDeseada = Vector3.zero;
            transform.position = new Vector3(posicionX, transform.position.y, transform.position.z);
        }
    }

    private void DetectarInput()
    {
        direccionInput = DireccionInput.Null;

        if (Input.GetKeyDown(KeyCode.A))
        {
            direccionInput = DireccionInput.Izquierda;
            carrilActual--;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            direccionInput = DireccionInput.Derecha;
            carrilActual++;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            direccionInput = DireccionInput.Abajo;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            direccionInput = DireccionInput.Arriba;
        }

        carrilActual = Mathf.Clamp(carrilActual, -1, 1);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Obstaculo")) //Debo cambiar el TAG a los obstaculos
        {
            if (gameManager.EstadoActual == EstadosDelJuego.GameOver)
            {
                return;
            }

            SoundManager.Instancia.ReproducirSonidoFX(SoundManager.Instancia.colisionClip);
            playerAnimaciones.MostrarAnimacionColision(); //animacion
            gameManager.CambiarEstado(EstadosDelJuego.GameOver);
        }
    }

    
}

